using System.Linq.Expressions;
using Domain.Dto;
using Domain.Models;
using Microsoft.Extensions.Logging;
using Repository.Interface;
using Service.Interface;

namespace Service.Implementation;

public class StationService(
    IRepository<Station> repository,
    IGeocodingApiClient geocoding,
    ILogger<StationService> logger) : IStationService
{
    // Static because StationService is scoped — a new instance per request would
    // otherwise each get their own lock and guard nothing.
    // SemaphoreSlim rather than lock(), because the guarded section awaits.
    private static readonly SemaphoreSlim Lock = new(1, 1);

    public Task<Station?> GetByIdAsync(Guid id) => repository.GetByIdAsync(id);

    public async Task<Station?> GetByNameAsync(string city, string? countryCode = null)
        => string.IsNullOrWhiteSpace(city) ? null
            : (await GetAllAsync(city, countryCode)).FirstOrDefault();

    public async Task<List<Station>> GetAllAsync(string? city = null, string? countryCode = null)
    {
        var country = NormalizeCountry(countryCode);

        // Only the country filter goes to the database. 
        Expression<Func<Station, bool>>? predicate =
            country == null ? null : s => s.CountryCode == country;

        var stations = await repository.GetAllAsync(
            s => s, predicate, 
            orderBy: q => q.OrderBy(s => s.City)
            );

        if (string.IsNullOrWhiteSpace(city))
            return stations;

        var name = city.Trim().ToLower();
        return stations.Where(s => s.City.ToLower() == name).ToList();
    }

    public Task<PaginatedResult<Station>> GetPagedAsync(int pageNumber, int pageSize)
        => repository.GetAllPagedAsync(s => s, pageNumber, pageSize,
            orderBy: q => q.OrderBy(s => s.City));

    public async Task<Station> DeleteByIdAsync(Guid id)
    {
        var station = await repository.GetByIdAsync(id)
                      ?? throw new KeyNotFoundException($"Station '{id}' was not found.");

        await repository.DeleteAsync(station);
        return station;
    }

    public async Task<Station?> AddByCityAsync(
        string cityName,
        string? countryCode = null,
        CancellationToken cancellationToken = default)
    {
        // Already stored? Return it and never touch the provider.
        // A city's coordinates do not change.
        var known = await GetByNameAsync(cityName, countryCode);
        if (known is not null)
        {
            logger.LogDebug("Station {City} already exists; skipping geocoding.", known.City);
            return known;
        }

        // Not stored, so we are about to insert. Serialize from here: without it,
        // two requests for the same new city can both look, both see nothing, and
        // both insert — and the unique index on ExternalId rejects the loser.
        //
        // The geocoder call sits inside the lock on purpose. Whoever waits will
        // find the city already stored on the second check and skip the lookup
        // entirely, so N simultaneous requests cost one API call, not N.
        await Lock.WaitAsync(cancellationToken);

        try
        {
            // Second check, now holding the lock:
            // someone may have added it while we were queued.
            known = await GetByNameAsync(cityName, countryCode);
            if (known is not null)
            {
                logger.LogDebug("Station {City} was added while waiting; skipping geocoding.", known.City);
                return known;
            }

            var place = await geocoding.SearchAsync(cityName, countryCode, cancellationToken);

            if (place is null)
            {
                logger.LogWarning(
                    "Cannot add station: no geocoding match for {City} (country {CountryCode}).",
                    cityName, countryCode ?? "any");
                return null;
            }

            // CountryCode is required on the entity. Every result in testing carried
            // one, but the provider does not contractually promise it.
            if (string.IsNullOrWhiteSpace(place.CountryCode))
            {
                logger.LogError(
                    "Geocoder returned {City} with no country code; cannot store it.", place.Name);
                return null;
            }

            // The name checks above use what the caller typed, but the geocoder
            // answers with its own spelling ("Victoria" comes back as "Vitoria"),
            // so they can miss a station we already have. The provider id is reliable.
            var station = (await repository.GetAllAsync(s => s, s => s.ExternalId == place.ExternalId))
                .FirstOrDefault();

            if (station is not null)
            {
                logger.LogDebug(
                    "Station {City} already stored as {StoredName}; skipping insert.",
                    cityName, station.City);

                return station;
            }

            // The geocoded coordinates are stored, NOT the ones the air-quality endpoint
            // echoes back — that response snaps to the provider's model grid (41.48456
            // becomes 41.5), and writing those back would make the station drift.
            station = new Station
            {
                City = place.Name,
                CountryCode = NormalizeCountry(place.CountryCode)!,
                Latitude = place.Latitude,
                Longitude = place.Longitude,
                ExternalId = place.ExternalId
            };

            await repository.InsertAsync(station);

            logger.LogInformation(
                "Added station {City} ({CountryCode}, externalId={ExternalId}) at {Lat},{Lon}.",
                station.City, station.CountryCode, station.ExternalId,
                station.Latitude, station.Longitude);

            return station;
        }
        finally
        {
            Lock.Release();
        }
    }

    private static string? NormalizeCountry(string? countryCode)
        => string.IsNullOrWhiteSpace(countryCode) ? null : countryCode.Trim().ToUpper();
}
