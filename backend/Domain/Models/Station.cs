using Domain.Common;

namespace Domain.Models;

public class Station : BaseAuditableEntity<string>
{
    public string City { get; set; } = null!;

    // two-letter code ("MK").
    public string CountryCode { get; set; } = null!;
    
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? ExternalId { get; set; }

    public virtual ICollection<Measurement> Measurements { get; set; } = [];
    public virtual ICollection<AlertSubscription> AlertSubscriptions { get; set; } = [];
}
