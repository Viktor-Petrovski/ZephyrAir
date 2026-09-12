using Domain.Common;
using Domain.Enums;

namespace Domain.Models;

public class InboundEventEntry : BaseEntity
{
    public string Payload { get; set; } = string.Empty;
    public InboundEventStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
}
