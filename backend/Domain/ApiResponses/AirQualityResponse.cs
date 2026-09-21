using System.Text.Json;
using System.Text.Json.Serialization;

namespace Domain.ApiResponses;

public class AirQualityResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }

    [JsonPropertyName("elevation")]
    public double? Elevation { get; set; }

    // e.g. { "time": "iso8601", "pm10": "μg/m³" }
    [JsonPropertyName("current_units")]
    public Dictionary<string, string>? CurrentUnits { get; set; }

    // Holds "time" (string), "interval" (number) and one entry per requested
    // pollutant, whose value is a number OR null for an unavailable grid point.
    [JsonPropertyName("current")]
    public Dictionary<string, JsonElement>? Current { get; set; }
}

// {
// "latitude": 41.5,
// "longitude": 22.100002,
// "generationtime_ms": 0.213742256164551,
// "utc_offset_seconds": 0,
// "timezone": "GMT",
// "timezone_abbreviation": "GMT",
// "elevation": 149,
// "current_units": {
//     "time": "iso8601",
//     "interval": "seconds",
//     "pm10": "μg/m³",
//     "pm2_5": "μg/m³"
// },
// "current": {
//     "time": "2026-09-19T13:00",
//     "interval": 3600,
//     "pm10": 14.3,
//     "pm2_5": 11.5
// }
// }