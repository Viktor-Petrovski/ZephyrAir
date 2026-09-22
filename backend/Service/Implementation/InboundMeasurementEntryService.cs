using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Domain.Dto;
using Domain.Enums;
using Domain.Models;
using Repository.Interface;
using Service.Interface;

namespace Service.Implementation;

public class InboundMeasurementEntryService(IRepository<InboundMeasurementEntry> repository)
    : IInboundMeasurementEntryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

        // Store "μg/m³" and Cyrillic as-is.
        // By default, every non-ASCII character is written as an escape code,
        // which reads back fine but makes the stored text unreadable.
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public async Task<InboundMeasurementEntry> CreateAsync(Guid stationId, AirQualitySnapshot snapshot)
    {
        var entry = new InboundMeasurementEntry
        {
            StationId = stationId,
            Payload = JsonSerializer.Serialize(snapshot, JsonOptions),
            Status = InboundMeasurementStatus.Pending,
            ReceivedAtUtc = DateTime.UtcNow
        };

        await repository.InsertAsync(entry);
        return entry;
    }

    public async Task<InboundMeasurementEntry> GetByIdNotNullAsync(Guid id)
        => await repository.GetByIdAsync(id)
           ?? throw new KeyNotFoundException($"Inbound measurement entry '{id}' was not found.");

    public async Task<List<InboundMeasurementEntry>> GetAllAsync(Guid? stationId, InboundMeasurementStatus? status)
        => await repository.GetAllAsync(
            selector: x => x,
            predicate: x => 
                (stationId == null || x.StationId == stationId) && (status == null || x.Status == status)
        );

    // Deserialize only returns null for a payload that is literally "null";
    // malformed JSON throws on its own.
    public AirQualitySnapshot ReadSnapshot(InboundMeasurementEntry entry)
        => JsonSerializer.Deserialize<AirQualitySnapshot>(entry.Payload, JsonOptions)
           ?? throw new InvalidOperationException(
               $"Inbound measurement entry '{entry.Id}' has no snapshot in its payload.");
    
}
