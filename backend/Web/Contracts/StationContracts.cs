namespace Web.Contracts;

// CountryCode is an optional ISO-3166 two-letter code ("MK"). Leave it out and
// the first match wins, which can be the wrong place entirely.
public record AddByCityRequest(string City, string? CountryCode);

// Station is never returned directly: lazy-loading proxies would pull in
// Measurements / AlertSubscriptions during serialization and cycle back to
// Station. This is the flat shape the API actually exposes.
public record StationResponse(
    Guid Id,
    string City,
    string? CountryCode,
    double Latitude,
    double Longitude,
    string? ExternalId);
