using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.ApiResponses;
using Domain.Dto;
using Service.Interface;

namespace Service.Implementation;

public class AirQualityApiClient(HttpClient httpClient) : IAirQualityApiClient
{
    public async Task<AirQualitySnapshot> FetchCurrentAsync(
        double latitude,
        double longitude,
        IReadOnlyCollection<string> pollutantCodes,
        CancellationToken cancellationToken = default)
    {
        if (pollutantCodes.Count == 0)
            throw new ArgumentException("At least one pollutant code is required.");

        // Escape each code, then join on literal commas so they stay separators.
        var codes = string.Join(',', pollutantCodes.Select(Uri.EscapeDataString));

        // Invariant so coordinates always use a dot:
        // 41.48456 would otherwise be written as "41,48456", which the provider rejects.
        var url = FormattableString.Invariant(
            $"v1/air-quality?latitude={latitude}&longitude={longitude}&current={codes}"
            );

        using var httpResponse = await httpClient.GetAsync(url, cancellationToken);

        // A single unknown pollutant code makes the provider reject the whole request.
        // Its reason names the bad code, so keep it in the error.
        if (!httpResponse.IsSuccessStatusCode)
        {
            var reason = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Open-Meteo rejected the request ({(int)httpResponse.StatusCode}): {reason}",
                null, httpResponse.StatusCode);
        }

        var response = await httpResponse.Content.ReadFromJsonAsync<AirQualityResponse>(cancellationToken);

        var current = response?.Current
            ?? throw new InvalidOperationException("Air-quality response has no 'current' block.");

        if (!current.TryGetValue("time", out var time) || time.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException("Air-quality response has no 'current.time'.");

        var snapshot = new AirQualitySnapshot
        {
            MeasuredAtUtc = DateTime.Parse(
                time.GetString()!,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
                )
        };

        foreach (var code in pollutantCodes)
        {
            // A grid point can report null for a pollutant.
            // Skip it rather than store a 0 that was never measured.
            if (!current.TryGetValue(code, out var value) || value.ValueKind != JsonValueKind.Number)
                continue;

            snapshot.Readings.Add(new PollutantReading
            {
                Code = code,
                Value = value.GetDouble(),
                Unit = response.CurrentUnits?.GetValueOrDefault(code) ?? string.Empty
            });
        }

        return snapshot;
    }
}
