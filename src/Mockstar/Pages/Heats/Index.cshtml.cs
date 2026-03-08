using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mockstar.Models;

namespace Mockstar.Pages.Heats;

public sealed class IndexModel : PageModel
{
    private static readonly JsonSerializerOptions StateJsonOptions = new(JsonSerializerDefaults.Web);

    public void OnGet()
    {
    }

    public IActionResult OnPostDetail(string heatId, string stateJson)
    {
        if (string.IsNullOrWhiteSpace(heatId) || string.IsNullOrWhiteSpace(stateJson))
        {
            return Partial("_HeatDetailPartial", HeatDetailViewModel.WithMessage("Choose an imported heat to inspect."));
        }

        var state = JsonSerializer.Deserialize<ClientJudgingState>(stateJson, StateJsonOptions);
        var heat = state?.EventRecords?.SelectMany(record => record.Heats).FirstOrDefault(item => item.Id == heatId);

        return heat is null
            ? Partial("_HeatDetailPartial", HeatDetailViewModel.WithMessage("The selected heat was not found in client state."))
            : Partial("_HeatDetailPartial", HeatDetailViewModel.From(heat));
    }
}

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
