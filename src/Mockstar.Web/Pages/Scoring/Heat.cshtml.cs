using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mockstar.ParserApi.Contracts;
using Mockstar.Web.Services.Heats;

namespace Mockstar.Web.Pages.Scoring;

public sealed class HeatModel : PageModel
{
    private readonly HeatApiClient _heatApiClient;

    public HeatModel(HeatApiClient heatApiClient)
    {
        _heatApiClient = heatApiClient;
    }

    public ScoringViewModel? ViewModel { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string heatId, string? role, CancellationToken cancellationToken)
    {
        var (heat, _) = await _heatApiClient.GetHeatByIdAsync(heatId, cancellationToken);

        if (heat is null)
        {
            ErrorMessage = "Heat not found.";
            return Page();
        }

        ViewModel = ScoringViewModel.From(heat, role);
        return Page();
    }

    public async Task<IActionResult> OnGetPanelAsync(string heatId, string? role, CancellationToken cancellationToken)
    {
        var (heat, _) = await _heatApiClient.GetHeatByIdAsync(heatId, cancellationToken);

        if (heat is null)
        {
            return Content("Heat not found.");
        }

        var viewModel = ScoringViewModel.From(heat, role);
        return Partial("_ScoringPanelPartial", viewModel);
    }
}

public sealed record ScoringViewModel(
    string HeatId,
    string HeatName,
    string HeatType,
    string ActiveRole,
    bool ShowRoleSwitcher,
    IReadOnlyList<RoleTab> Roles,
    IReadOnlyList<ScoringEntry> Entries)
{
    public static ScoringViewModel From(ParserHeat heat, string? requestedRole)
    {
        var roles = GetAvailableRoles(heat);
        var activeRole = roles.Contains(requestedRole ?? "", StringComparer.Ordinal)
            ? requestedRole!
            : roles.FirstOrDefault() ?? "couple";

        var entries = GetEntriesForRole(heat, activeRole);

        return new ScoringViewModel(
            heat.Id,
            heat.Name,
            FormatHeatType(heat.Type),
            activeRole,
            roles.Count > 1,
            roles.Select(r => new RoleTab(r, FormatRole(r), r == activeRole)).ToList(),
            entries.Select((e, i) => new ScoringEntry(e.Id, e.Display, i + 1, 500)).ToList());
    }

    private static IReadOnlyList<string> GetAvailableRoles(ParserHeat heat)
    {
        if (heat.Type == "JackAndJillPrelimHeat")
        {
            var roles = new List<string>();
            if (heat.LeaderEntries.Count > 0) roles.Add("leader");
            if (heat.FollowerEntries.Count > 0) roles.Add("follower");
            return roles;
        }

        return ["couple"];
    }

    private static IReadOnlyList<ParserBibEntry> GetEntriesForRole(ParserHeat heat, string role)
    {
        return role switch
        {
            "leader" => heat.LeaderEntries,
            "follower" => heat.FollowerEntries,
            _ => heat.Type == "StrictlyHeat"
                ? heat.CoupleEntries.Select(c => new ParserBibEntry(c.Id, c.LeaderBib, c.Display)).ToList()
                : heat.LeaderEntries
        };
    }

    private static string FormatHeatType(string type) =>
        type switch
        {
            "JackAndJillPrelimHeat" => "Jack & Jill Prelim",
            "JackAndJillFinalHeat" => "Jack & Jill Final",
            "StrictlyHeat" => "Strictly",
            _ => type
        };

    private static string FormatRole(string role) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(role);
}

public sealed record RoleTab(string Id, string Label, bool IsActive);

public sealed record ScoringEntry(string EntryId, string Display, int Rank, int Score);
