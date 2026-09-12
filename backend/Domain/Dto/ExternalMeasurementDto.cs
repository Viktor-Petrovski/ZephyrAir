namespace Domain.Dto;

// One spot reading as returned by the external air-quality provider.
public class ExternalMeasurementDto
{
    public string StationExternalId { get; set; } = null!;
    public string StationName { get; set; } = null!;
    public string City { get; set; } = null!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string PollutantCode { get; set; } = null!;
    public double Value { get; set; }
    public DateTime MeasuredAtUtc { get; set; }
}
