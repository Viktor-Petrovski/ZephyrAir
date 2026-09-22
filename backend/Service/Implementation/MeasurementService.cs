using System.Linq.Expressions;
using Domain.Dto;
using Domain.Models;
using Microsoft.Extensions.Logging;
using Repository.Interface;
using Service.Interface;

namespace Service.Implementation;

public class MeasurementService(
    IRepository<Measurement> repository,
    ILogger<MeasurementService> logger) : IMeasurementService
{
    public async Task<Measurement> GetByIdNotNullAsync(Guid id)
        => await repository.GetByIdAsync(id)
           ?? throw new KeyNotFoundException($"Measurement '{id}' was not found.");

    public async Task<Measurement?> GetLatestAsync(Guid stationId, Guid pollutantId)
    {
        // Paged rather than GetAllAsync: this becomes a Take(1) in SQL instead of
        // materializing every reading the station has ever had.
        var page = await repository.GetAllPagedAsync(
            m => m,
            pageNumber: 1,
            pageSize: 1,
            predicate: m => m.StationId == stationId && m.PollutantId == pollutantId,
            orderBy: q => q.OrderByDescending(m => m.MeasuredAtUtc));

        return page.Items.FirstOrDefault();
    }

    public Task<List<Measurement>> GetAllAsync(
        Guid? stationId = null,
        Guid? pollutantId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null)
        => repository.GetAllAsync(
            m => m,
            BuildFilter(stationId, pollutantId, fromUtc, toUtc),
            orderBy: q => q.OrderByDescending(m => m.MeasuredAtUtc));

    public Task<PaginatedResult<Measurement>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? stationId = null,
        Guid? pollutantId = null)
        => repository.GetAllPagedAsync(
            m => m,
            pageNumber,
            pageSize,
            BuildFilter(stationId, pollutantId, null, null),
            orderBy: q => q.OrderByDescending(m => m.MeasuredAtUtc));

    public async Task<Measurement?> RecordAsync(
        Guid stationId, Guid pollutantId, double value, DateTime measuredAtUtc)
    {
        var timestamp = NormaliseToUtc(measuredAtUtc);

        // Only one row per station + pollutant + time is allowed
        var alreadySaved = await repository.ExistsAsync(
            m => m.StationId == stationId
                 && m.PollutantId == pollutantId
                 && m.MeasuredAtUtc == timestamp);

        if (alreadySaved)
        {
            logger.LogDebug(
                "Measurement already recorded for station {StationId}, pollutant {PollutantId} at {Timestamp:O}.",
                stationId, pollutantId, timestamp);

            return null;
        }

        var measurement = new Measurement
        {
            StationId = stationId,
            PollutantId = pollutantId,
            Value = value,
            MeasuredAtUtc = timestamp
        };

        // On several servers, two saves could pass the check at once; the database would reject one.
        await repository.InsertAsync(measurement);

        return measurement;
    }

    private static Expression<Func<Measurement, bool>> BuildFilter(
        Guid? stationId, Guid? pollutantId, DateTime? fromUtc, DateTime? toUtc)
    {
        var from = fromUtc.HasValue ? NormaliseToUtc(fromUtc.Value) : (DateTime?)null;
        var to = toUtc.HasValue ? NormaliseToUtc(toUtc.Value) : (DateTime?)null;

        return m =>
            (!stationId.HasValue || m.StationId == stationId.Value)
            && (!pollutantId.HasValue || m.PollutantId == pollutantId.Value)
            && (!from.HasValue || m.MeasuredAtUtc >= from.Value)
            && (!to.HasValue || m.MeasuredAtUtc <= to.Value);
    }

    private static DateTime NormaliseToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
