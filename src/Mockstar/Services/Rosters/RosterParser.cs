using System.Text.RegularExpressions;
using Mockstar.Domain;

namespace Mockstar.Services.Rosters;

public sealed partial class RosterParser
{
    public ParsedRosterDocument Parse(string input)
    {
        var normalized = NormalizeInput(input);
        var lines = normalized.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var eventName = lines.FirstOrDefault() ?? "Imported Event";
        var divisionKind = DetectDivisionKind(normalized);
        var phase = DetectPhase(normalized);
        var divisionName = DetectDivisionName(normalized);

        var heats = ParseHeatSections(normalized, divisionKind);
        if (heats.Count == 0 || heats.All(IsEmptyHeat))
        {
            throw new InvalidOperationException("No roster entries found in the provided text.");
        }

        return new ParsedRosterDocument(eventName, divisionName, divisionKind, phase, heats);
    }

    private static string NormalizeInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Roster text is required.", nameof(input));
        }

        return input.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Trim();
    }

    private static DivisionKind DetectDivisionKind(string text) =>
        text.Contains("strict", StringComparison.OrdinalIgnoreCase)
            ? DivisionKind.StrictlySwing
            : DivisionKind.JackAndJill;

    private static RoundPhase DetectPhase(string text)
    {
        if (text.Contains("quarter", StringComparison.OrdinalIgnoreCase))
        {
            return RoundPhase.Quarterfinal;
        }

        if (text.Contains("semi", StringComparison.OrdinalIgnoreCase))
        {
            return RoundPhase.Semifinal;
        }

        if (text.Contains("final", StringComparison.OrdinalIgnoreCase))
        {
            return RoundPhase.Final;
        }

        return RoundPhase.Prelim;
    }

    private static string DetectDivisionName(string text)
    {
        var match = DivisionNameRegex().Match(text);
        return match.Success ? match.Value.Trim() : "Imported Division";
    }

    private static List<ParsedHeat> ParseHeatSections(string text, DivisionKind divisionKind)
    {
        var headers = HeatSplitRegex().Matches(text);
        var heats = new List<ParsedHeat>();

        if (headers.Count == 0)
        {
            heats.Add(ParseHeatBody("Heat 1", text, divisionKind));
            return heats;
        }

        for (var index = 0; index < headers.Count; index++)
        {
            var header = headers[index];
            var bodyStart = header.Index + header.Length;
            var bodyEnd = index + 1 < headers.Count ? headers[index + 1].Index : text.Length;
            var body = text[bodyStart..bodyEnd];
            heats.Add(ParseHeatBody(header.Groups[1].Value.Trim(), body, divisionKind));
        }

        return heats;
    }

    private static ParsedHeat ParseHeatBody(string heatName, string body, DivisionKind divisionKind)
    {
        if (divisionKind is DivisionKind.StrictlySwing)
        {
            var couples = CouplePatternRegex()
                .Matches(body)
                .Select(match => new ParsedCouple(
                    int.Parse(match.Groups[1].Value),
                    int.Parse(match.Groups[2].Value)))
                .Distinct()
                .ToArray();

            return new ParsedHeat(heatName, Array.Empty<int>(), Array.Empty<int>(), couples, Array.Empty<int>());
        }

        var headers = RoleHeaderRegex().Matches(body);
        if (headers.Count == 0)
        {
            return new ParsedHeat(
                heatName,
                Array.Empty<int>(),
                Array.Empty<int>(),
                Array.Empty<ParsedCouple>(),
                ExtractUniqueBibs(body));
        }

        if (headers.Count == 1 && IsCombinedRoleHeader(headers[0].Value))
        {
            var rolePairs = ExtractCombinedRoleRows(body[(headers[0].Index + headers[0].Length)..]);
            return new ParsedHeat(
                heatName,
                rolePairs.Leaders,
                rolePairs.Followers,
                Array.Empty<ParsedCouple>(),
                Array.Empty<int>());
        }

        var leaders = new HashSet<int>();
        var followers = new HashSet<int>();

        for (var index = 0; index < headers.Count; index++)
        {
            var header = headers[index];
            var sectionStart = header.Index + header.Length;
            var sectionEnd = index + 1 < headers.Count ? headers[index + 1].Index : body.Length;
            var sectionText = body[sectionStart..sectionEnd];
            var sectionBibs = ExtractUniqueBibs(sectionText);

            if (header.Value.Contains("lead", StringComparison.OrdinalIgnoreCase))
            {
                leaders.UnionWith(sectionBibs);
            }
            else
            {
                followers.UnionWith(sectionBibs);
            }
        }

        return new ParsedHeat(
            heatName,
            leaders.OrderBy(bib => bib).ToArray(),
            followers.OrderBy(bib => bib).ToArray(),
            Array.Empty<ParsedCouple>(),
            Array.Empty<int>());
    }

    private static int[] ExtractUniqueBibs(string text) =>
        BibPatternRegex()
            .Matches(text)
            .Select(match => int.Parse(match.Value))
            .Distinct()
            .OrderBy(bib => bib)
            .ToArray();

    private static bool IsCombinedRoleHeader(string header) =>
        header.Contains("leader", StringComparison.OrdinalIgnoreCase)
        && (header.Contains("follower", StringComparison.OrdinalIgnoreCase)
            || header.Contains("follow", StringComparison.OrdinalIgnoreCase));

    private static (int[] Leaders, int[] Followers) ExtractCombinedRoleRows(string body)
    {
        var leaders = new List<int>();
        var followers = new List<int>();

        foreach (var line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var matches = BibPatternRegex().Matches(line);
            if (matches.Count < 2)
            {
                continue;
            }

            leaders.Add(int.Parse(matches[0].Value));
            followers.Add(int.Parse(matches[^1].Value));
        }

        return (
            leaders.Distinct().OrderBy(bib => bib).ToArray(),
            followers.Distinct().OrderBy(bib => bib).ToArray());
    }

    private static bool IsEmptyHeat(ParsedHeat heat) =>
        heat.LeaderBibs.Count == 0
        && heat.FollowerBibs.Count == 0
        && heat.Couples.Count == 0
        && heat.AmbiguousBibs.Count == 0;

    [GeneratedRegex(@"(?:^|\n)\s*(Heat\s+\d+[:\s-]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex HeatSplitRegex();

    [GeneratedRegex(@"(\d{1,4})\s*(?:[+/&-]|and)\s*(\d{1,4})", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex CouplePatternRegex();

    [GeneratedRegex(@"\b\d{1,4}\b", RegexOptions.Compiled)]
    private static partial Regex BibPatternRegex();

    [GeneratedRegex(@"(novice|newcomer|intermediate|advanced|all-star|champion|masters)[^\n]*", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex DivisionNameRegex();

    [GeneratedRegex(@"(?im)^[^\S\n]*[#\w()/-]*(?:[^\S\n]+[\w#()/-]+)*[^\S\n]*\b(leader(?:s)?|follower(?:s)?|follow(?:s)?)\b[^\n]*$")]
    private static partial Regex RoleHeaderRegex();
}
