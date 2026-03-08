using Mockstar.ParserApi.Contracts;
using Mockstar.Scoring;

namespace Mockstar.ParserApi.Services.Rosters;

internal static class ParserImportResponseFactory
{
    public static ParserImportResponse Create(ParsedRosterDocument parsed, NormalizedRoster normalized, string sourceText)
    {
        var heats = normalized.EventRecord.Heats.Select(CreateHeatReview).ToArray();
        var prompts = normalized.RoleAssignmentPrompts
            .Select(prompt => new ParserRolePrompt(
                prompt.HeatName,
                normalized.EventRecord.Heats.First(heat => heat.Name == prompt.HeatName).Id,
                prompt.Entries.Select(entry => entry.Display).ToArray()))
            .ToArray();

        return new ParserImportResponse(
            sourceText,
            normalized.EventRecord.Name,
            heats,
            prompts,
            ToParserEventRecord(parsed, normalized.EventRecord));
    }

    private static ParserHeatReview CreateHeatReview(Heat heat) =>
        heat switch
        {
            JackAndJillFinalHeat finalHeat => new ParserHeatReview(
                finalHeat.Name,
                "Jack and Jill Final",
                finalHeat.LeaderEntries.Select(entry => entry.Display).ToArray(),
                finalHeat.FollowerEntries.Select(entry => entry.Display).ToArray(),
                Array.Empty<string>()),
            JackAndJillPrelimHeat prelimHeat => new ParserHeatReview(
                prelimHeat.Name,
                $"Jack and Jill {prelimHeat.Phase}",
                prelimHeat.LeaderEntries.Select(entry => entry.Display).ToArray(),
                prelimHeat.FollowerEntries.Select(entry => entry.Display).ToArray(),
                Array.Empty<string>()),
            StrictlyHeat strictlyHeat => new ParserHeatReview(
                strictlyHeat.Name,
                $"Strictly {strictlyHeat.Phase}",
                Array.Empty<string>(),
                Array.Empty<string>(),
                strictlyHeat.CoupleEntries.Select(entry => entry.Display).ToArray()),
            _ => throw new InvalidOperationException($"Unsupported heat type {heat.GetType().Name}.")
        };

    private static ParserEventRecord ToParserEventRecord(ParsedRosterDocument parsed, EventRecord eventRecord)
    {
        var heats = eventRecord.Heats
            .Zip(parsed.Heats, (heat, parsedHeat) =>
            {
                var type = heat switch
                {
                    JackAndJillFinalHeat => "jack-and-jill-final",
                    JackAndJillPrelimHeat => "jack-and-jill-prelim",
                    StrictlyHeat => "strictly",
                    _ => throw new InvalidOperationException($"Unsupported heat type {heat.GetType().Name}.")
                };

                var phase = heat.Phase.ToString().ToLowerInvariant();
                var importSource = new ParserImportSource(
                    heat.ImportSource.Kind.ToString().ToLowerInvariant(),
                    heat.ImportSource.Timestamp.ToString("O"));

                return heat switch
                {
                    JackAndJillFinalHeat finalHeat => new ParserHeat(
                        finalHeat.Id,
                        finalHeat.Name,
                        type,
                        phase,
                        finalHeat.LeaderEntries.Select(entry => new ParserBibEntry(entry.Id, entry.Bib, entry.Display)).ToArray(),
                        finalHeat.FollowerEntries.Select(entry => new ParserBibEntry(entry.Id, entry.Bib, entry.Display)).ToArray(),
                        Array.Empty<ParserCoupleEntry>(),
                        finalHeat.Pairings.Select(pairing => new ParserPairing(pairing.LeaderBib, pairing.FollowerBib)).ToArray(),
                        parsedHeat.AmbiguousBibs.Select(bib => new ParserBibEntry($"bib-{bib}", bib, bib.ToString())).ToArray(),
                        importSource),
                    JackAndJillPrelimHeat prelimHeat => new ParserHeat(
                        prelimHeat.Id,
                        prelimHeat.Name,
                        type,
                        phase,
                        prelimHeat.LeaderEntries.Select(entry => new ParserBibEntry(entry.Id, entry.Bib, entry.Display)).ToArray(),
                        prelimHeat.FollowerEntries.Select(entry => new ParserBibEntry(entry.Id, entry.Bib, entry.Display)).ToArray(),
                        Array.Empty<ParserCoupleEntry>(),
                        Array.Empty<ParserPairing>(),
                        parsedHeat.AmbiguousBibs.Select(bib => new ParserBibEntry($"bib-{bib}", bib, bib.ToString())).ToArray(),
                        importSource),
                    StrictlyHeat strictlyHeat => new ParserHeat(
                        strictlyHeat.Id,
                        strictlyHeat.Name,
                        type,
                        phase,
                        Array.Empty<ParserBibEntry>(),
                        Array.Empty<ParserBibEntry>(),
                        strictlyHeat.CoupleEntries.Select(entry => new ParserCoupleEntry(entry.Id, entry.LeaderBib, entry.FollowerBib, entry.Display)).ToArray(),
                        Array.Empty<ParserPairing>(),
                        Array.Empty<ParserBibEntry>(),
                        importSource),
                    _ => throw new InvalidOperationException($"Unsupported heat type {heat.GetType().Name}.")
                };
            })
            .ToArray();

        return new ParserEventRecord(eventRecord.Id, eventRecord.Name, heats);
    }
}
