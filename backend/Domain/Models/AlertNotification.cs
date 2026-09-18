using Domain.Common;
using Domain.Enums;

namespace Domain.Models;

public class AlertNotification : BaseEntity
{
    public Guid AlertSubscriptionId { get; set; }
    public virtual AlertSubscription AlertSubscription { get; set; } = null!;

    public double TriggeringValue { get; set; }
    public DateTime SentAtUtc { get; set; }
    public NotificationChannel Channel { get; set; }
}
