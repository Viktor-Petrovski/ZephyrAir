namespace Domain.Dto;

// A city resolved to coordinates by the external geocoding provider.
// Consumed by StationService to create/update a Station.
public class GeocodedPlace
{
    // The provider's own id (e.g. "787716"), stored as Station.ExternalId.
    public string ExternalId { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? CountryCode { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
