using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mockstar.Web.Models;

namespace Mockstar.Web.Pages.Scoring;

public sealed class IndexModel : PageModel
{
    private static readonly JsonSerializerOptions StateJsonOptions = new(JsonSerializerDefaults.Web);

    public void OnGet()
    {
    }

    public IActionResult OnPostShell(string stateJson, string? activeRole)
    {
        var state = DeserializeState(stateJson);
        return Partial("_ScoringShellPartial", ScoringShellViewModel.From(state, activeRole));
    }

    private static ClientJudgingState? DeserializeState(string? stateJson)
    {
        if (string.IsNullOrWhiteSpace(stateJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ClientJudgingState>(stateJson, StateJsonOptions);
    }
}

public sealed record ScoringShellViewModel(
    string? Message,
    string? ActiveRole,
    string? HeatType,
    string? HeatName,
    string? RoleSummary,
    string? PairingHeatId,
    bool ShowRoleSwitcher,
    bool ShowPairingPanel,
    bool FinalizeDisabled,
    IReadOnlyList<ScoringRoleOptionViewModel> Roles,
    IReadOnlyList<ScoringPairOptionViewModel> LeaderOptions,
    IReadOnlyList<ScoringPairOptionViewModel> FollowerOptions,
    IReadOnlyList<ScoringRowViewModel> Rows)
{
    public static ScoringShellViewModel From(ClientJudgingState? state, string? activeRole)
    {
        if (state is null || string.IsNullOrWhiteSpace(state.SelectedHeatId))
        {
            return Empty("Import a roster and pick a heat before scoring.");
        }

        var heat = state.EventRecords.SelectMany(record => record.Heats).FirstOrDefault(item => item.Id == state.SelectedHeatId);
        if (heat is null)
        {
            return Empty("The selected heat could not be found in client state.");
        }

        var roles = AvailableRoles(heat);
        if (roles.Count == 0)
        {
            return Empty("The selected heat does not have scoreable entries yet.");
        }

        var normalizedRole = roles.Contains(activeRole ?? string.Empty, StringComparer.Ordinal)
            ? activeRole!
            : roles[0];
        var sheet = state.ScoreSheets.FirstOrDefault(item =>
            string.Equals(item.HeatId, heat.Id, StringComparison.Ordinal)
            && string.Equals(item.Role, normalizedRole, StringComparison.Ordinal));
        if (sheet is null)
        {
            return Empty("The score sheet for the selected role is unavailable.");
        }

        var entries = GetEntriesForRole(heat, sheet.Role);
        var ranked = RankEntries(heat, entries, sheet.Scores);
        var ranking = ranked.Select((entry, index) => new { entry.Id, Rank = index + 1 })
            .ToDictionary(item => item.Id, item => item.Rank, StringComparer.Ordinal);
        var tiedIds = GetTiedEntryIds(sheet.Scores);

        return new(
            null,
            normalizedRole,
            heat.Type.Replace("-", " ", StringComparison.Ordinal),
            heat.Name,
            BuildRoleSummary(sheet),
            heat.Id,
            roles.Count > 1,
            string.Equals(heat.Type, "jack-and-jill-final", StringComparison.Ordinal) && heat.FollowerEntries.Count > 0,
            string.Equals(sheet.Status, "finalized", StringComparison.Ordinal) || tiedIds.Count > 0,
            roles.Select(role => new ScoringRoleOptionViewModel(role, RoleLabel(role), string.Equals(role, normalizedRole, StringComparison.Ordinal))).ToArray(),
            heat.LeaderEntries.Select(entry => new ScoringPairOptionViewModel(entry.Bib, entry.Display)).ToArray(),
            heat.FollowerEntries.Select(entry => new ScoringPairOptionViewModel(entry.Bib, entry.Display)).ToArray(),
            entries.Select(entry => new ScoringRowViewModel(
                entry.Id,
                EntryDisplay(heat, entry),
                ranking[entry.Id],
                sheet.Scores.TryGetValue(entry.Id, out var score) ? score : 500,
                tiedIds.Contains(entry.Id, StringComparer.Ordinal),
                string.Equals(sheet.Status, "finalized", StringComparison.Ordinal))).ToArray());
    }

    private static ScoringShellViewModel Empty(string message) =>
        new(
            message,
            null,
            null,
            null,
            null,
            null,
            false,
            false,
            true,
            Array.Empty<ScoringRoleOptionViewModel>(),
            Array.Empty<ScoringPairOptionViewModel>(),
            Array.Empty<ScoringPairOptionViewModel>(),
            Array.Empty<ScoringRowViewModel>());

    private static IReadOnlyList<string> AvailableRoles(ClientHeat heat)
    {
        if (string.Equals(heat.Type, "jack-and-jill-prelim", StringComparison.Ordinal))
        {
            return new[]
                {
                    heat.LeaderEntries.Count > 0 ? "leader" : null,
                    heat.FollowerEntries.Count > 0 ? "follower" : null
                }
                .OfType<string>()
                .ToArray();
        }

        return new[] { "couple" };
    }

    private static IReadOnlyList<ClientBibEntry> GetEntriesForRole(ClientHeat heat, string role)
    {
        var entries = role switch
        {
            "leader" => heat.LeaderEntries,
            "follower" => heat.FollowerEntries,
            _ => string.Equals(heat.Type, "strictly", StringComparison.Ordinal)
                ? heat.CoupleEntries.Select(entry => new ClientBibEntry(entry.Id, entry.LeaderBib, entry.Display)).ToArray()
                : heat.LeaderEntries
        };

        return entries
            .OrderBy(entry => EntryDisplay(heat, entry), StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<ClientBibEntry> RankEntries(
        ClientHeat heat,
        IReadOnlyList<ClientBibEntry> entries,
        IReadOnlyDictionary<string, int> scores) =>
        entries
            .OrderByDescending(entry => scores.TryGetValue(entry.Id, out var score) ? score : 500)
            .ThenBy(entry => EntryDisplay(heat, entry), StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static IReadOnlyList<string> GetTiedEntryIds(IReadOnlyDictionary<string, int> scores) =>
        scores
            .GroupBy(pair => pair.Value)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Select(pair => pair.Key))
            .ToArray();

    private static string EntryDisplay(ClientHeat heat, ClientBibEntry entry)
    {
        if (string.Equals(heat.Type, "jack-and-jill-final", StringComparison.Ordinal))
        {
            var pairing = heat.Pairings.FirstOrDefault(pair => pair.LeaderBib == entry.Bib);
            return pairing is null ? entry.Display : $"{entry.Display}/{pairing.FollowerBib}";
        }

        return entry.Display;
    }

    private static string BuildRoleSummary(ClientScoreSheet sheet) =>
        $"Scoring role: {RoleLabel(sheet.Role)}. Status: {sheet.Status}{(string.IsNullOrWhiteSpace(sheet.FinalizedAt) ? string.Empty : $" at {sheet.FinalizedAt}")}";

    private static string RoleLabel(string role) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(role);
}

public sealed record ScoringRoleOptionViewModel(string Id, string Label, bool IsActive);

public sealed record ScoringPairOptionViewModel(int Bib, string Display);

public sealed record ScoringRowViewModel(
    string EntryId,
    string Display,
    int Rank,
    int Score,
    bool IsTied,
    bool IsDisabled);
