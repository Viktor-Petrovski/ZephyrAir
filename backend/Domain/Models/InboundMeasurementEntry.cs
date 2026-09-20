using Domain.Common;
using Domain.Enums;

namespace Domain.Models;

public class InboundMeasurementEntry : BaseEntity
{
    public string Payload { get; set; } = string.Empty;
    public InboundMeasurementStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
}
