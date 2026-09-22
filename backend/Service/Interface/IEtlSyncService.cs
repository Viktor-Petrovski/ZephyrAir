namespace Service.Interface;

public interface IEtlSyncService
{
    // Pulls the latest readings from the external provider and queues them in
    // InboundMeasurementEntries, recording the run in EtlSyncLog.
    Task SyncAllAsync(CancellationToken cancellationToken = default);
}
