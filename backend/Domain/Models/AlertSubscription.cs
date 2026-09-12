using Domain.Common;

namespace Domain.Models;

public class AlertSubscription : BaseAuditableEntity<string>
{
    public required string UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    public Guid StationId { get; set; }
    public virtual Station Station { get; set; } = null!;

    public Guid PollutantId { get; set; }
    public virtual Pollutant Pollutant { get; set; } = null!;

    public double ThresholdValue { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<AlertNotification> Notifications { get; set; } = [];
}
