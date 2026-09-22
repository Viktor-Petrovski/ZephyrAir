using Quartz;
using Service.Interface;

namespace Service.Jobs;

/// A slow run must finish before the next one starts,
/// or two runs would pick up the same Pending rows.
[DisallowConcurrentExecution]
public class InboundMeasurementEntryJob(IInboundMeasurementEntryProcessor processor) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken) =>
        await processor.ProcessPendingEntriesAsync(cancellationToken);
    
}