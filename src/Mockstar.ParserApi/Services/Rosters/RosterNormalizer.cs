using System.Text;
using Mockstar.Scoring;
using Mockstar.Services.Rosters;

namespace Mockstar.ParserApi.Services.Rosters;

public sealed class RosterNormalizer
{
    private readonly Func<DateTimeOffset> _clock;

    public RosterNormalizer()
        : this(() => DateTimeOffset.UtcNow)
    {
    }

    public RosterNormalizer(Func<DateTimeOffset> clock)
    {
        _clock = clock;
    }

    public NormalizedRoster Normalize(ParsedRosterDocument parsed, ImportSourceKind importSourceKind)
    {
        var timestamp = _clock();
        var eventId = $"{Slugify(parsed.EventName)}-{timestamp:yyyyMMddHHmmss}";
        var importSource = new ImportSource(importSourceKind, timestamp);
        var prompts = new List<RoleAssignmentPrompt>();
        var heats = new List<Heat>(parsed.Heats.Count);

        for (var index = 0; index < parsed.Heats.Count; index++)
        {
            var parsedHeat = parsed.Heats[index];
            var heatId = $"{eventId}-heat-{index + 1}";
            var heatName = $"{parsed.DivisionName} {parsedHeat.Name}".Trim();

            if (parsedHeat.HasAmbiguousRoles)
            {
                prompts.Add(new RoleAssignmentPrompt(
                    heatName,
                    parsedHeat.AmbiguousBibs.Select(CreateBibEntry).ToArray()));
            }

            heats.Add(CreateHeat(parsedHeat, parsed.Phase, heatId, heatName, importSource));
        }

        return new NormalizedRoster(new EventRecord(eventId, parsed.EventName, heats), prompts);
    }

    private static Heat CreateHeat(
        ParsedHeat parsedHeat,
        RoundPhase phase,
        string heatId,
        string heatName,
        ImportSource importSource)
    {
        if (parsedHeat.Couples.Count > 0)
        {
            return new StrictlyHeat(
                heatId,
                heatName,
                phase,
                parsedHeat.Couples
                    .Select(couple => new CoupleEntry(
                        $"couple-{couple.LeaderBib}-{couple.FollowerBib}",
                        couple.LeaderBib,
                        couple.FollowerBib))
                    .ToArray(),
                importSource);
        }

        var leaderEntries = parsedHeat.LeaderBibs.Select(CreateBibEntry).ToArray();
        var followerEntries = parsedHeat.FollowerBibs.Select(CreateBibEntry).ToArray();

        return phase is RoundPhase.Final
            ? new JackAndJillFinalHeat(heatId, heatName, leaderEntries, followerEntries, Array.Empty<Pairing>(), importSource)
            : new JackAndJillPrelimHeat(heatId, heatName, phase, leaderEntries, followerEntries, importSource);
    }

    private static BibEntry CreateBibEntry(int bib) => new($"bib-{bib}", bib);

    private static string Slugify(string value)
    {
        var builder = new StringBuilder();
        var previousDash = false;

        foreach (var character in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousDash = false;
            }
            else if (!previousDash)
            {
                builder.Append('-');
                previousDash = true;
            }
        }

        return builder.ToString().Trim('-');
    }
}
