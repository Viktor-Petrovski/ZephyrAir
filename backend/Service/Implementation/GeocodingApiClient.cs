using System.Globalization;
using System.Net.Http.Json;
using Domain.Dto;
using Domain.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Domain.ApiResponses;
using Service.Interface;

namespace Service.Implementation;

public class GeocodingApiClient(
    HttpClient httpClient,
    IOptions<OpenMeteoOptions> options,
    IMemoryCache cache,
    ILogger<GeocodingApiClient> logger) : IGeocodingApiClient
{
    // A city name never resolves to a different place, so the answer keeps.
    // A miss keeps for far less: a typo should not be stuck for a day.
    private static readonly TimeSpan HitLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan MissLifetime = TimeSpan.FromMinutes(5);

    public async Task<GeocodedPlace?> SearchAsync(
        string cityName,
        string? countryCode = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cityName))
            throw new ArgumentException("City name is required.", nameof(cityName));

        // Keyed on what the CALLER typed. The database stores the place, but nothing
        // in it records that the word "Skopje" means Скопје — only the provider
        // knows that, so without this we re-ask on every lookup of a city whose
        // stored spelling differs from the typed one.
        var cacheKey = $"geo:{cityName.Trim().ToLower()}|{countryCode?.Trim().ToUpper() ?? "*"}";

        if (cache.TryGetValue(cacheKey, out GeocodedPlace? cached))
        {
            logger.LogDebug("Geocoding cache hit for {City}.", cityName);
            return cached;
        }

        var url = $"v1/search?name={Uri.EscapeDataString(cityName)}&count=1&format=json";

        if (!string.IsNullOrWhiteSpace(countryCode))
            url += $"&countryCode={Uri.EscapeDataString(countryCode.Trim().ToUpperInvariant())}";

        // Without a language the provider only matches Latin input, so Cyrillic
        // returns nothing. It also decides the spelling that comes back.
        var language = options.Value.Language;
        if (!string.IsNullOrWhiteSpace(language))
            url += $"&language={Uri.EscapeDataString(language.Trim().ToLowerInvariant())}";

        var response = await httpClient.GetFromJsonAsync<GeocodingResponse>(url, cancellationToken);

        // "results" is omitted entirely when nothing matches, so this covers both
        // a missing array and an empty one.
        var match = response?.Results?.FirstOrDefault();
        if (match is null)
        {
            logger.LogWarning(
                "Geocoding returned no match for city {City} (country {CountryCode}).",
                cityName, countryCode ?? "any");

            cache.Set(cacheKey, (GeocodedPlace?)null, MissLifetime);
            return null;
        }

        var place = new GeocodedPlace
        {
            ExternalId = match.Id.ToString(CultureInfo.InvariantCulture),
            Name = match.Name,
            CountryCode = match.CountryCode,
            Latitude = match.Latitude,
            Longitude = match.Longitude
        };

        cache.Set(cacheKey, place, HitLifetime);
        return place;
    }
}
