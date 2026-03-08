namespace Mockstar.Models;

public sealed record ClientJudgingState(
    IReadOnlyList<ClientEventRecord> EventRecords,
    IReadOnlyList<ClientScoreSheet> ScoreSheets,
    string? SelectedHeatId,
    IReadOnlyList<string> LastImportedHeatIds);

public sealed record ClientEventRecord(string Id, string Name, IReadOnlyList<ClientHeat> Heats);

public sealed record ClientHeat(
    string Id,
    string Name,
    string Type,
    string Phase,
    IReadOnlyList<ClientBibEntry> LeaderEntries,
    IReadOnlyList<ClientBibEntry> FollowerEntries,
    IReadOnlyList<ClientCoupleEntry> CoupleEntries,
    IReadOnlyList<ClientPairing> Pairings,
    IReadOnlyList<ClientBibEntry> AmbiguousEntries,
    ClientImportSource ImportSource);

public sealed record ClientBibEntry(string Id, int Bib, string Display);

public sealed record ClientCoupleEntry(string Id, int LeaderBib, int? FollowerBib, string Display);

public sealed record ClientPairing(int LeaderBib, int FollowerBib);

public sealed record ClientImportSource(string Kind, string Timestamp);

public sealed record ClientScoreSheet(
    string HeatId,
    string Role,
    string Status,
    IReadOnlyDictionary<string, int> Scores,
    string? FinalizedAt);
