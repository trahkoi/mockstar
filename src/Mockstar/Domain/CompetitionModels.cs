using System.Globalization;

namespace Mockstar.Domain;

public enum DivisionKind
{
    JackAndJill,
    StrictlySwing
}

public enum RoundPhase
{
    Prelim,
    Quarterfinal,
    Semifinal,
    Final
}

public enum ScoringRole
{
    Leader,
    Follower,
    Couple
}

public enum HeatStatus
{
    Draft,
    Finalized
}

public enum ImportSourceKind
{
    Image,
    Web
}

public sealed record BibEntry(string Id, int Bib)
{
    public string Display => Bib.ToString(CultureInfo.InvariantCulture);
}

public sealed record CoupleEntry(string Id, int LeaderBib, int? FollowerBib)
{
    public string Display =>
        FollowerBib is int followerBib
            ? $"{LeaderBib.ToString(CultureInfo.InvariantCulture)}/{followerBib.ToString(CultureInfo.InvariantCulture)}"
            : LeaderBib.ToString(CultureInfo.InvariantCulture);
}

public sealed record Pairing(int LeaderBib, int FollowerBib);

public sealed record ImportSource(ImportSourceKind Kind, DateTimeOffset Timestamp);

public abstract record Heat(
    string Id,
    string Name,
    RoundPhase Phase,
    DivisionKind DivisionKind,
    ImportSource ImportSource);

public sealed record JackAndJillPrelimHeat(
    string Id,
    string Name,
    RoundPhase Phase,
    IReadOnlyList<BibEntry> LeaderEntries,
    IReadOnlyList<BibEntry> FollowerEntries,
    ImportSource ImportSource)
    : Heat(Id, Name, Phase, DivisionKind.JackAndJill, ImportSource);

public sealed record JackAndJillFinalHeat(
    string Id,
    string Name,
    IReadOnlyList<BibEntry> LeaderEntries,
    IReadOnlyList<BibEntry> FollowerEntries,
    IReadOnlyList<Pairing> Pairings,
    ImportSource ImportSource)
    : Heat(Id, Name, RoundPhase.Final, DivisionKind.JackAndJill, ImportSource);

public sealed record StrictlyHeat(
    string Id,
    string Name,
    RoundPhase Phase,
    IReadOnlyList<CoupleEntry> CoupleEntries,
    ImportSource ImportSource)
    : Heat(Id, Name, Phase, DivisionKind.StrictlySwing, ImportSource);

public sealed record ScoreSheet(
    string HeatId,
    ScoringRole Role,
    HeatStatus Status,
    IReadOnlyDictionary<string, int> Scores,
    DateTimeOffset? FinalizedAt);

public sealed record EventRecord(
    string Id,
    string Name,
    IReadOnlyList<Heat> Heats);
