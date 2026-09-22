using Quartz;
using Service.Interface;

namespace Service.Jobs;

/// A slow run must finish before the next one starts,
/// or two runs would queue the same stations twice.
[DisallowConcurrentExecution]
public class EtlJob(IEtlSyncService service ) : IJob
{
    public async ValueTask Execute
        (IJobExecutionContext context, CancellationToken cancellationToken)
    {
        await service.SyncAllAsync(cancellationToken);
    }
}