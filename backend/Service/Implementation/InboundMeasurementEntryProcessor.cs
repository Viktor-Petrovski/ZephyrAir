using Domain.Enums;
using Domain.Models;
using Microsoft.Extensions.Logging;
using Repository.Interface;
using Service.Interface;

namespace Service.Implementation;

/// The save half of the pipeline. It reads the snapshots EtlSyncService queued,
/// saves their readings as Measurements, and marks each entry Completed or Failed.
public class InboundMeasurementEntryProcessor(
    IInboundMeasurementEntryService inboundService,
    IPollutantService pollutantService,
    IMeasurementService measurementService,
    IRepository<InboundMeasurementEntry> repository,
    ILogger<InboundMeasurementEntryProcessor> logger) : IInboundMeasurementEntryProcessor
{
    public async Task ProcessPendingEntriesAsync(CancellationToken cancellationToken = default)
    {
        // Paged rather than inboundService.GetAllAsync, so the limit is applied in SQL:
        // a backlog (say, after a day offline) is worked off 100 at a time instead of
        // loaded all at once. Oldest first, matching ix_inbound_status_received.
        var page = await repository.GetAllPagedAsync(
            e => e,
            pageNumber: 1,
            pageSize: 100,
            predicate: e => e.Status == InboundMeasurementStatus.Pending,
            orderBy: q => q.OrderBy(e => e.ReceivedAtUtc));

        // The normal case between hourly fetches; nothing worth logging.
        if (page.Items.Count == 0)
            return;

        var inserted = 0;
        var failed = 0;

        foreach (var entry in page.Items)
        {
            // Checked between entries only, so a shutdown never leaves one half-saved.
            cancellationToken.ThrowIfCancellationRequested();

            inserted += (await ProcessMeasurementEntryAsync(entry)).Count;

            if (entry.Status == InboundMeasurementStatus.Failed)
                failed++;
        }

        logger.LogInformation(
            "Inbound processing finished: processed {Processed} of {Pending} pending entries, " +
            "{Failed} failed, {Inserted} new measurement(s).",
            page.Items.Count, page.TotalCount, failed, inserted);
    }

    public async Task<IReadOnlyList<Measurement>> ProcessMeasurementEntryAsync(InboundMeasurementEntry entry)
    {
        var inserted = new List<Measurement>();

        try
        {
            var snapshot = inboundService.ReadSnapshot(entry);

            // One query for all pollutants, instead of one per reading.
            var pollutants = (await pollutantService.GetAllAsync()).ToDictionary(p => p.Code);

            foreach (var reading in snapshot.Readings)
            {
                // The fetch only asks for codes in the Pollutants table, so this means the
                // payload came from somewhere else. Pollutants are never created here.
                if (!pollutants.TryGetValue(reading.Code, out var pollutant))
                {
                    logger.LogWarning(
                        "Skipping unknown pollutant {Code} in inbound entry {EntryId}.",
                        reading.Code, entry.Id);
                    continue;
                }

                MeasurementUnit? reportedUnit = reading.Unit switch
                {
                    "μg/m³" => MeasurementUnit.MicrogramsPerCubicMeter,
                    "EAQI" or "USAQI" => MeasurementUnit.Index,
                    _ => null
                };

                // Saved anyway: the number itself is real.
                // The warning means the pollutant's configured unit needs fixing.
                if (reportedUnit != pollutant.Unit)
                    logger.LogWarning(
                        "Unit mismatch for {Code} in inbound entry {EntryId}: provider says '{ReportedUnit}', " +
                        "pollutant is set to {Unit}. Saving anyway.",
                        reading.Code, entry.Id, reading.Unit, pollutant.Unit);

                // Null when this reading was already saved.
                var measurement = await measurementService.RecordAsync(
                    entry.StationId, pollutant.Id, reading.Value, snapshot.MeasuredAtUtc);

                if (measurement is not null)
                    inserted.Add(measurement);
            }

            entry.Status = InboundMeasurementStatus.Completed;
            entry.ErrorMessage = null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // No transaction: readings saved before the failure are kept, and
            // processing the entry again skips them as duplicates.
            entry.Status = InboundMeasurementStatus.Failed;
            entry.ErrorMessage = ex.Message;

            logger.LogError(ex,
                "Inbound entry {EntryId} for station {StationId} failed.",
                entry.Id, entry.StationId);
        }

        entry.ProcessedAtUtc = DateTime.UtcNow;
        await repository.UpdateAsync(entry);

        // Returned even when the entry failed: those rows are saved, and processing
        // again would skip them as duplicates, so this is their only chance at an alert.
        return inserted;
    }
}
