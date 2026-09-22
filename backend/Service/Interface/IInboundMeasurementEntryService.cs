using Domain.Dto;
using Domain.Enums;
using Domain.Models;

namespace Service.Interface;

public interface IInboundMeasurementEntryService
{
    /// Drops a station's snapshot into the queue, unprocessed (Pending).
    Task<InboundMeasurementEntry> CreateAsync(Guid stationId, AirQualitySnapshot snapshot);
    Task<InboundMeasurementEntry> GetByIdNotNullAsync(Guid id);
    
    /// Optional filtering with stationId and status
    Task<List<InboundMeasurementEntry>> GetAllAsync(Guid? stationId, InboundMeasurementStatus? status);

    /// Reads an entry's payload back into a snapshot, with the same JSON settings
    /// CreateAsync wrote it with. Throws when the payload cannot be read.
    AirQualitySnapshot ReadSnapshot(InboundMeasurementEntry entry);
}
