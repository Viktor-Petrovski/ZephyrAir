using Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Domain.Configuration;
using Service.Interface;

namespace Service.Implementation;

public class ReferenceDataSeeder(
    IPollutantService pollutants,
    IStationService stations,
    IOptions<OpenMeteoOptions> options,
    ILogger<ReferenceDataSeeder> logger) : IReferenceDataSeeder
{
    private static readonly (string Code, string DisplayName, MeasurementUnit Unit)[] Catalog =
    [
        ("pm10",             "PM10",             MeasurementUnit.MicrogramsPerCubicMeter),
        ("pm2_5",            "PM2.5",            MeasurementUnit.MicrogramsPerCubicMeter),
        ("carbon_monoxide",  "Carbon monoxide",  MeasurementUnit.MicrogramsPerCubicMeter),
        ("nitrogen_dioxide", "Nitrogen dioxide", MeasurementUnit.MicrogramsPerCubicMeter),
        ("sulphur_dioxide",  "Sulphur dioxide",  MeasurementUnit.MicrogramsPerCubicMeter),
        ("ozone",            "Ozone",            MeasurementUnit.MicrogramsPerCubicMeter)
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedPollutantsAsync(cancellationToken);
        await SeedBootstrapCitiesAsync(cancellationToken);
    }

    private async Task SeedPollutantsAsync(CancellationToken cancellationToken)
    {
        var created = 0;

        foreach (var (code, displayName, unit) in Catalog)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await pollutants.GetByCodeAsync(code) is not null)
                continue;

            await pollutants.CreateAsync(code, displayName, unit);
            created++;

            logger.LogInformation("Seeded pollutant {Code} ({DisplayName}).", code, displayName);
        }

        var total = (await pollutants.GetAllAsync()).Count;
        logger.LogInformation(
            "Reference data seeded: {Created} pollutant(s) created, {Total} total.", created, total);
    }

    private async Task SeedBootstrapCitiesAsync(CancellationToken cancellationToken)
    {
        var cities = options.Value.BootstrapCities;
        if (cities.Length == 0)
            return;

        var existing = await stations.GetAllAsync();
        if (existing.Count > 0)
        {
            logger.LogInformation(
                "Stations already provisioned ({Count}); skipping bootstrap cities.", existing.Count);
            return;
        }

        var created = 0;

        foreach (var city in cities)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (await stations.AddByCityAsync(
                        city, options.Value.BootstrapCountryCode, cancellationToken) is not null)
                    created++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A provider outage must not stop the application from starting —
                // the city can be added later via POST /api/stations/by-city.
                logger.LogError(ex, "Failed to bootstrap city {City}.", city);
            }
        }

        logger.LogInformation(
            "Bootstrap cities: {Created} of {Requested} station(s) provisioned.", created, cities.Length);
    }
}
