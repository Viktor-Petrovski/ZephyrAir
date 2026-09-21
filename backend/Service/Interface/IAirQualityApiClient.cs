using Domain.Dto;

namespace Service.Interface;

public interface IAirQualityApiClient
{
    /// Reads the provider's current readings for one point into a snapshot.
    Task<AirQualitySnapshot> FetchCurrentAsync(
        double latitude,
        double longitude,
        IReadOnlyCollection<string> pollutantCodes,
        CancellationToken cancellationToken = default);
}
