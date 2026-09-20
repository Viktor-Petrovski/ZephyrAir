using System.Text.Json.Serialization;

namespace Domain.ApiResponses;

public class GeocodingResponse
{
    // Absent entirely (not an empty array) when the provider has no match.
    [JsonPropertyName("results")]
    public List<GeocodingResult>? Results { get; init; }
}

public class GeocodingResult
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }
}
