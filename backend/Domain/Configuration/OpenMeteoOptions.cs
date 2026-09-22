namespace Domain.Configuration;

// Bound from the "OpenMeteo" section of appsettings.json.
public class OpenMeteoOptions
{
    public const string SectionName = "OpenMeteo";

    public string GeocodingBaseUrl { get; set; } = "https://geocoding-api.open-meteo.com/";
    public string AirQualityBaseUrl { get; set; } = "https://air-quality-api.open-meteo.com/";

    public int RequestTimeoutSeconds { get; set; } = 20;

    // Language the geocoder answers in, e.g. "mk". It also decides what a search
    // MATCHES: without it, Cyrillic input returns nothing at all. Setting it means
    // stations are stored under their local spelling (Скопје, not Skopje).
    // Null leaves the provider default.
    public string? Language { get; set; }

    // Cities geocoded into Stations on first run, so a fresh database has a work list.
    public string[] BootstrapCities { get; set; } = [];

    // Applied to every bootstrap city, so "Ohrid" cannot resolve to the Bulgarian
    // one. Null means "first match wins".
    public string? BootstrapCountryCode { get; set; }
}
