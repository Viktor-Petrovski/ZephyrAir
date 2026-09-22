using Domain.Models;

namespace Service.Interface;

public interface IInboundMeasurementEntryProcessor
{
    /// Processes up to 100 Pending entries, oldest first.
    Task ProcessPendingEntriesAsync(CancellationToken cancellationToken = default);

    /// Saves one entry's readings as Measurements and marks it Completed or Failed.
    /// Returns only the newly inserted measurements —
    /// readings already stored are skipped, and only new ones should trigger alerts.
    Task<IReadOnlyList<Measurement>> ProcessMeasurementEntryAsync(InboundMeasurementEntry entry);
}
