using Domain.Common;

namespace Domain.Models;

public class Measurement : BaseEntity
{
    public Guid StationId { get; set; }
    public virtual Station Station { get; set; } = null!;

    public Guid PollutantId { get; set; }
    public virtual Pollutant Pollutant { get; set; } = null!;

    public double Value { get; set; }
    public DateTime MeasuredAtUtc { get; set; }
}
