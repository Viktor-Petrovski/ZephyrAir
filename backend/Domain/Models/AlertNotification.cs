using Domain.Common;

namespace Domain.Models;

public class AlertNotification : BaseEntity
{
    public Guid AlertSubscriptionId { get; set; }
    public virtual AlertSubscription AlertSubscription { get; set; } = null!;

    public double TriggeringValue { get; set; }
    public DateTime SentAtUtc { get; set; }
    public string? Channel { get; set; }
}
