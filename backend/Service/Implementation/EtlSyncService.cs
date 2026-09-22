using Domain.Models;
using Microsoft.Extensions.Logging;
using Repository.Interface;
using Service.Interface;

namespace Service.Implementation;

/// The fetch half of the pipeline. It pulls each station's current readings as a
/// snapshot and drops it into the inbound queue — it never touches Measurement.
/// Saving the readings is InboundMeasurementEntryProcessor's job.
public class EtlSyncService(
    IStationService stationService,
    IPollutantService pollutantService,
    IAirQualityApiClient airQuality,
    IInboundMeasurementEntryService inboundService,
    IRepository<EtlSyncLog> repository,
    ILogger<EtlSyncService> logger) : IEtlSyncService
{
    private const string JobName = "open-meteo-fetch";

    public async Task SyncAllAsync(CancellationToken cancellationToken = default)
    {
        // Read first, so the log records the run's size from the start —
        // even a run that crashes midway shows how many stations it had.
        var stations = await stationService.GetAllAsync();

        var log = new EtlSyncLog
        {
            JobName = JobName,
            StartedAt = DateTime.UtcNow,
            StationsTotal = stations.Count
        };

        await repository.InsertAsync(log);

        var queued = 0;
        var total = stations.Count;
        var failures = new List<string>();
        string? fatal = null;

        try
        {
            var codes = (await pollutantService.GetAllAsync())
                .Select(p => p.Code).ToList();

            if (codes.Count == 0)
            {
                // Nothing to ask the provider for. Treat as a run failure rather
                // than a silent no-op: it means reference data never seeded.
                fatal = "No pollutants are configured; nothing to fetch.";
                logger.LogError("ETL sync aborted: {Reason}", fatal);
                return;
            }

            if (total == 0)
                logger.LogWarning("ETL sync has no stations to poll; the work list is empty.");

            foreach (var station in stations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var snapshot = await airQuality.FetchCurrentAsync(
                        station.Latitude, station.Longitude, 
                        codes, cancellationToken
                        );

                    await inboundService.CreateAsync(station.Id, snapshot);
                    queued++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // One station failing must not abort the run — the others are
                    // independent, and a partial hour beats no hour.
                    failures.Add($"{station.City} ({station.Id}): {ex.Message}");
                    logger.LogError(ex,
                        "ETL fetch failed for station {City} ({StationId}).",
                        station.City, station.Id);
                }
            }
        }
        catch (OperationCanceledException)
        {
            fatal = "Run was cancelled.";
            logger.LogWarning("ETL sync cancelled after queueing {Queued} of {Total}.",
                queued, total);
            throw;
        }
        catch (Exception ex)
        {
            fatal = ex.Message;
            logger.LogError(ex, "ETL sync failed.");
            throw;
        }
        finally
        {
            var success = fatal is null && failures.Count == 0;
    
            log.CompletedAt = DateTime.UtcNow;
            log.Success = success;

            // Stations QUEUED, not measurements stored —
            // that happens later, under the inbound-processing job.
            log.StationsFetched = queued;

            log.ErrorMessage = success
                ? null
                : fatal ?? $"Queued {queued} of {total}. Failures: " +
                $"{string.Join(" | ", failures)}";

            await repository.UpdateAsync(log);

            logger.LogInformation(
                "ETL sync finished: queued {Queued} of {Total} payload(s), {Failed} failure(s).",
                queued, total, failures.Count);
        }
    }
}
