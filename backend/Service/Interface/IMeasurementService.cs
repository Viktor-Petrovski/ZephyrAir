using Domain.Dto;
using Domain.Models;

namespace Service.Interface;

public interface IMeasurementService
{
    Task<Measurement> GetByIdNotNullAsync(Guid id);
    Task<Measurement?> GetLatestAsync(Guid stationId, Guid pollutantId);

    Task<List<Measurement>> GetAllAsync(
        Guid? stationId = null,
        Guid? pollutantId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null);

    Task<PaginatedResult<Measurement>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? stationId = null,
        Guid? pollutantId = null);

    /// Saves a reading and returns the new row. Returns null when the same reading
    /// (same station, pollutant and time) was already saved, so nothing is saved twice.
    Task<Measurement?> RecordAsync(
        Guid stationId, Guid pollutantId, double value, DateTime measuredAtUtc);
}
