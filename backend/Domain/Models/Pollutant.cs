using Domain.Common;
using Domain.Enums;

namespace Domain.Models;

public class Pollutant : BaseAuditableEntity<string>
{
    public string Code { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public MeasurementUnit Unit { get; set; }

    public virtual ICollection<Measurement> Measurements { get; set; } = [];
    public virtual ICollection<AlertSubscription> AlertSubscriptions { get; set; } = [];
}
