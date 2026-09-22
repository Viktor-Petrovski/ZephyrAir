using Domain.Dto;
using Domain.Models;

namespace Service.Interface;

public interface IStationService
{
    Task<Station?> GetByIdAsync(Guid id);

    /// countryCode is optional: null means any country, first match wins.
    Task<Station?> GetByNameAsync(string city, string? countryCode = null);

    Task<List<Station>> GetAllAsync(string? city = null, string? countryCode = null);

    Task<PaginatedResult<Station>> GetPagedAsync(int pageNumber, int pageSize);

    /// The single "add a city" path, shared by the admin endpoint and the bootstrap
    /// seeder. Checks the database first and only calls the geocoder when the city
    /// is not already stored — a city's coordinates never change, so there is
    /// nothing to refresh. Returns null when the provider has no match.
    Task<Station?> AddByCityAsync(
        string cityName,
        string? countryCode = null,
        CancellationToken cancellationToken = default);

    /// Stops tracking a city. This also deletes every measurement recorded for it (cascade),
    /// and is refused outright while an alert subscription or an inbound entry points at it (restrict).
    /// Throws KeyNotFoundException if the station does not exist.
    Task<Station> DeleteByIdAsync(Guid id);
}
