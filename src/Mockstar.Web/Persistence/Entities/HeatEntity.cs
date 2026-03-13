using Mockstar.Scoring;

namespace Mockstar.Web.Persistence.Entities;

public class HeatEntity
{
    public int Id { get; set; }
    public string HeatId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public RoundPhase Phase { get; set; }
    public DivisionKind DivisionKind { get; set; }
    public string HeatType { get; set; } = string.Empty; // "JackAndJillPrelim", "JackAndJillFinal", "Strictly"

    // Import source
    public ImportSourceKind ImportSourceKind { get; set; }
    public DateTimeOffset ImportTimestamp { get; set; }

    // JSON-serialized data for entries (flexible storage)
    public string EntriesJson { get; set; } = string.Empty;

    // Foreign key
    public string EventId { get; set; } = string.Empty;
    public EventEntity Event { get; set; } = null!;
}
