using Domain.Dto;

namespace Service.Interface;

public interface IAirQualityApiClient
{
    Task<List<ExternalMeasurementDto>> FetchLatestAsync(CancellationToken cancellationToken = default);
}
