using Domain.Dto;

namespace Service.Interface;

public interface IGeocodingApiClient
{
    /// Resolves a city name to coordinates.
    /// Returns null when the provider has no match.
    Task<GeocodedPlace?> SearchAsync(
        string cityName,
        string? countryCode = null,
        CancellationToken cancellationToken = default);
}
