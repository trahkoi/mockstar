using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mockstar.Web.Models;

namespace Mockstar.Web.Pages.Heats;

public sealed class IndexModel : PageModel
{
    private static readonly JsonSerializerOptions StateJsonOptions = new(JsonSerializerDefaults.Web);

    public void OnGet()
    {
    }

    public IActionResult OnPostDetail(string heatId, string stateJson)
    {
        var state = DeserializeState(stateJson);
        if (string.IsNullOrWhiteSpace(heatId) || state is null)
        {
            return Partial("_HeatDetailPartial", HeatDetailViewModel.WithMessage("Choose an imported heat to inspect."));
        }

        var heat = state?.EventRecords?.SelectMany(record => record.Heats).FirstOrDefault(item => item.Id == heatId);

        return heat is null
            ? Partial("_HeatDetailPartial", HeatDetailViewModel.WithMessage("The selected heat was not found in client state."))
            : Partial("_HeatDetailPartial", HeatDetailViewModel.From(heat));
    }

    public IActionResult OnPostList(string stateJson)
    {
        var state = DeserializeState(stateJson);
        return Partial("_HeatListPartial", HeatListViewModel.From(state));
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

public sealed record HeatListViewModel(
    string? Message,
    IReadOnlyList<HeatEventSectionViewModel> EventSections,
    string? SelectedHeatId)
{
    public static HeatListViewModel From(ClientJudgingState? state)
    {
        if (state is null || state.EventRecords.Count == 0)
        {
            return new("Import a roster first to populate heat selection.", Array.Empty<HeatEventSectionViewModel>(), null);
        }

        return new(
            null,
            state.EventRecords.Select(record => new HeatEventSectionViewModel(
                record.Name,
                record.Heats.Select(heat => new HeatListItemViewModel(heat.Id, heat.Name)).ToArray())).ToArray(),
            state.SelectedHeatId);
    }
}

public sealed record HeatEventSectionViewModel(string EventName, IReadOnlyList<HeatListItemViewModel> Heats);

public sealed record HeatListItemViewModel(string Id, string Name);

public sealed record HeatDetailViewModel(
    string? Message,
    string? Name,
    string? Type,
    IReadOnlyList<string> LeaderEntries,
    IReadOnlyList<string> FollowerEntries,
    IReadOnlyList<string> CoupleEntries)
{
    public static HeatDetailViewModel WithMessage(string message) =>
        new(message, null, null, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());

    public static HeatDetailViewModel From(ClientHeat heat) =>
        new(
            null,
            heat.Name,
            heat.Type.Replace("-", " ", StringComparison.Ordinal),
            heat.LeaderEntries.Select(entry => entry.Display).ToArray(),
            heat.FollowerEntries.Select(entry => entry.Display).ToArray(),
            heat.CoupleEntries.Select(entry => entry.Display).ToArray());
}
