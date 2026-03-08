using Mockstar.Scoring;

namespace Mockstar.Services.Rosters;

public sealed record ParsedCouple(int LeaderBib, int FollowerBib);

public sealed record ParsedHeat(
    string Name,
    IReadOnlyList<int> LeaderBibs,
    IReadOnlyList<int> FollowerBibs,
    IReadOnlyList<ParsedCouple> Couples,
    IReadOnlyList<int> AmbiguousBibs)
{
    public bool HasAmbiguousRoles => AmbiguousBibs.Count > 0;
}

public sealed record ParsedRosterDocument(
    string EventName,
    string DivisionName,
    DivisionKind DivisionKind,
    RoundPhase Phase,
    IReadOnlyList<ParsedHeat> Heats);

public sealed record RoleAssignmentPrompt(string HeatName, IReadOnlyList<BibEntry> Entries);

public sealed record NormalizedRoster(EventRecord EventRecord, IReadOnlyList<RoleAssignmentPrompt> RoleAssignmentPrompts);
