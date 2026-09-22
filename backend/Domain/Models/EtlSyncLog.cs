using Domain.Common;

namespace Domain.Models;

public class EtlSyncLog : BaseEntity
{
    public string JobName { get; set; } = null!;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    public bool? Success { get; set; }

    // How many stations there were to fetch (at the moment of StartedAt).
    public int StationsTotal { get; set; }

    // How many stations were fetched successfully; 
    public int? StationsFetched { get; set; }
    
    public string? ErrorMessage { get; set; }
}
