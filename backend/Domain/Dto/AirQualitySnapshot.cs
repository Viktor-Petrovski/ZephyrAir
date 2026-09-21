namespace Domain.Dto;

// The current air quality at one point, as read from the provider.
public class AirQualitySnapshot
{
    // One timestamp for every reading. UTC — the provider reports GMT.
    public DateTime MeasuredAtUtc { get; set; }

    public List<PollutantReading> Readings { get; set; } = [];
}

public class PollutantReading
{
    // The provider's variable name, matching Pollutant.Code ("pm10").
    public string Code { get; set; } = null!;

    public double Value { get; set; }

    // As the provider reports it ("μg/m³").
    // Kept so a pollutant configured with the wrong unit can be caught —
    // european_aqi reports "EAQI", an index, not a weight.
    public string Unit { get; set; } = null!;
}
