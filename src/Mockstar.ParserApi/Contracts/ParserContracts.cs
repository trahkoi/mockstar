namespace Mockstar.ParserApi.Contracts;

public sealed record ParseRosterTextRequest(string Text);

public sealed record ParseRosterUrlRequest(string Url);

public sealed record ParserImportResponse(
    string SourceText,
    string EventName,
    IReadOnlyList<ParserHeatReview> Heats,
    IReadOnlyList<ParserRolePrompt> RolePrompts,
    ParserEventRecord EventRecord);

public sealed record ParserHeatReview(
    string Name,
    string Kind,
    IReadOnlyList<string> LeaderEntries,
    IReadOnlyList<string> FollowerEntries,
    IReadOnlyList<string> CoupleEntries);

public sealed record ParserRolePrompt(string HeatName, string HeatId, IReadOnlyList<string> Entries);

public sealed record ParserEventRecord(string Id, string Name, IReadOnlyList<ParserHeat> Heats);

public sealed record ParserHeat(
    string Id,
    string Name,
    string Type,
    string Phase,
    IReadOnlyList<ParserBibEntry> LeaderEntries,
    IReadOnlyList<ParserBibEntry> FollowerEntries,
    IReadOnlyList<ParserCoupleEntry> CoupleEntries,
    IReadOnlyList<ParserPairing> Pairings,
    IReadOnlyList<ParserBibEntry> AmbiguousEntries,
    ParserImportSource ImportSource);

public sealed record ParserBibEntry(string Id, int Bib, string Display);

public sealed record ParserCoupleEntry(string Id, int LeaderBib, int? FollowerBib, string Display);

public sealed record ParserPairing(int LeaderBib, int FollowerBib);

public sealed record ParserImportSource(string Kind, string Timestamp);
