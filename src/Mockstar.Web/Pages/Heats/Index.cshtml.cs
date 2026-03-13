using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mockstar.ParserApi.Contracts;
using Mockstar.Web.Services.Heats;

namespace Mockstar.Web.Pages.Heats;

public sealed class IndexModel : PageModel
{
    private readonly HeatApiClient _heatApiClient;

    public IndexModel(HeatApiClient heatApiClient)
    {
        _heatApiClient = heatApiClient;
    }

    public HeatsViewModel ViewModel { get; private set; } = HeatsViewModel.Empty;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViewModel = await HeatsViewModel.LoadAsync(_heatApiClient, cancellationToken);
    }

    public async Task<IActionResult> OnGetDetailAsync(string heatId, CancellationToken cancellationToken)
    {
        var (heat, _) = await _heatApiClient.GetHeatByIdAsync(heatId, cancellationToken);

        return heat is null
            ? Partial("_HeatDetailPartial", HeatDetailViewModel.WithMessage("Heat not found."))
            : Partial("_HeatDetailPartial", HeatDetailViewModel.From(heat));
    }
}

public sealed record HeatsViewModel(
    IReadOnlyList<HeatEventSectionViewModel> EventSections)
{
    public static readonly HeatsViewModel Empty = new([]);

    public bool HasEvents => EventSections.Count > 0;

    public static async Task<HeatsViewModel> LoadAsync(HeatApiClient client, CancellationToken cancellationToken)
    {
        var eventIds = await client.ListEventIdsAsync(cancellationToken);
        if (eventIds.Count == 0)
        {
            return Empty;
        }

        var sections = new List<HeatEventSectionViewModel>();
        foreach (var eventId in eventIds)
        {
            var eventRecord = await client.GetEventAsync(eventId, cancellationToken);
            if (eventRecord is null)
            {
                continue;
            }

            sections.Add(new HeatEventSectionViewModel(
                eventRecord.Name,
                eventRecord.Heats.Select(h => new HeatListItemViewModel(h.Id, h.Name)).ToArray()));
        }

        return new HeatsViewModel(sections);
    }
}

public sealed record HeatEventSectionViewModel(string EventName, IReadOnlyList<HeatListItemViewModel> Heats);

public sealed record HeatListItemViewModel(string Id, string Name);

public sealed record HeatDetailViewModel(
    string? Message,
    string? HeatId,
    string? Name,
    string? Type,
    IReadOnlyList<string> LeaderEntries,
    IReadOnlyList<string> FollowerEntries,
    IReadOnlyList<string> CoupleEntries)
{
    public static HeatDetailViewModel WithMessage(string message) =>
        new(message, null, null, null, [], [], []);

    public static HeatDetailViewModel From(ParserHeat heat) =>
        new(
            null,
            heat.Id,
            heat.Name,
            heat.Type.Replace("-", " ", StringComparison.Ordinal),
            heat.LeaderEntries.Select(e => e.Display).ToArray(),
            heat.FollowerEntries.Select(e => e.Display).ToArray(),
            heat.CoupleEntries.Select(e => e.Display).ToArray());
}
