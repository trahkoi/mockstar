using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mockstar.ParserApi.Contracts;
using Mockstar.Web.Services.Heats;
using Mockstar.Web.Services.Imports;

namespace Mockstar.Web.Pages.Import;

public sealed class IndexModel : PageModel
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ParserApiClient _parserApiClient;
    private readonly HeatApiClient _heatApiClient;

    public IndexModel(ParserApiClient parserApiClient, HeatApiClient heatApiClient)
    {
        _parserApiClient = parserApiClient;
        _heatApiClient = heatApiClient;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostWebAsync(string url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return Partial("_ImportReview", ImportReviewViewModel.WithError("Enter a roster URL to scrape."));
        }

        var result = await _parserApiClient.ParseUrlAsync(url, cancellationToken);
        return BuildReviewResult(result);
    }

    public async Task<IActionResult> OnPostText(string extractedText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(extractedText))
        {
            return Partial("_ImportReview", ImportReviewViewModel.WithError("OCR did not produce any roster text."));
        }

        var result = await _parserApiClient.ParseTextAsync(extractedText, cancellationToken);
        return BuildReviewResult(result);
    }

    public async Task<IActionResult> OnPostActivateAsync(string activationPayload, string? roleAssignmentsJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(activationPayload))
        {
            TempData["Error"] = "Missing activation payload.";
            return RedirectToPage();
        }

        ParserEventRecord eventRecord;
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(activationPayload));
            eventRecord = JsonSerializer.Deserialize<ParserEventRecord>(json, JsonOptions)!;
        }
        catch
        {
            TempData["Error"] = "Invalid activation payload.";
            return RedirectToPage();
        }

        if (!string.IsNullOrWhiteSpace(roleAssignmentsJson))
        {
            var assignments = JsonSerializer.Deserialize<Dictionary<string, Dictionary<int, string>>>(roleAssignmentsJson, JsonOptions);
            if (assignments is not null)
            {
                eventRecord = ApplyRoleAssignments(eventRecord, assignments);
            }
        }

        var saved = await _heatApiClient.SaveEventAsync(eventRecord.Id, eventRecord, cancellationToken);
        if (!saved)
        {
            TempData["Error"] = "Could not save heats to the database. Is the parser API running?";
            return RedirectToPage();
        }

        return RedirectToPage("/Heats/Index");
    }

    private static ParserEventRecord ApplyRoleAssignments(
        ParserEventRecord eventRecord,
        Dictionary<string, Dictionary<int, string>> assignments)
    {
        var updatedHeats = eventRecord.Heats.Select(heat =>
        {
            if (heat.AmbiguousEntries.Count == 0 || !assignments.TryGetValue(heat.Id, out var heatAssignments))
            {
                return heat;
            }

            var leaders = heat.LeaderEntries.ToList();
            var followers = heat.FollowerEntries.ToList();

            for (var i = 0; i < heat.AmbiguousEntries.Count; i++)
            {
                if (!heatAssignments.TryGetValue(i, out var role))
                {
                    continue;
                }

                var entry = heat.AmbiguousEntries[i];
                if (string.Equals(role, "Leader", StringComparison.OrdinalIgnoreCase))
                {
                    leaders.Add(entry);
                }
                else if (string.Equals(role, "Follower", StringComparison.OrdinalIgnoreCase))
                {
                    followers.Add(entry);
                }
            }

            leaders.Sort((a, b) => a.Bib.CompareTo(b.Bib));
            followers.Sort((a, b) => a.Bib.CompareTo(b.Bib));

            return heat with
            {
                LeaderEntries = leaders,
                FollowerEntries = followers,
                AmbiguousEntries = []
            };
        }).ToArray();

        return eventRecord with { Heats = updatedHeats };
    }

    private PartialViewResult BuildReviewResult(ParserApiCallResult result)
    {
        if (!result.IsSuccess || result.Response is null)
        {
            return Partial("_ImportReview", ImportReviewViewModel.WithError(result.ErrorMessage ?? "Parsing service is temporarily unavailable."));
        }

        return Partial("_ImportReview", ImportReviewViewModel.From(result.Response));
    }
}

public sealed record ImportReviewViewModel(
    string? ErrorMessage,
    string? SourceText,
    string? EventName,
    IReadOnlyList<HeatReviewViewModel> Heats,
    IReadOnlyList<RolePromptViewModel> RolePrompts,
    string? ActivationPayload)
{
    private static readonly JsonSerializerOptions ActivationJsonOptions = new(JsonSerializerDefaults.Web);
    public bool HasData => ErrorMessage is null && Heats.Count > 0;

    public static ImportReviewViewModel WithError(string message) =>
        new(message, null, null, Array.Empty<HeatReviewViewModel>(), Array.Empty<RolePromptViewModel>(), null);

    public static ImportReviewViewModel From(ParserImportResponse response)
    {
        var heats = response.Heats.Select(HeatReviewViewModel.From).ToArray();
        var prompts = response.RolePrompts
            .Select(prompt => new RolePromptViewModel(prompt.HeatName, prompt.HeatId, prompt.Entries))
            .ToArray();
        var activationPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response.EventRecord, ActivationJsonOptions)));

        return new ImportReviewViewModel(null, response.SourceText, response.EventName, heats, prompts, activationPayload);
    }
}

public sealed record HeatReviewViewModel(
    string Name,
    string Kind,
    IReadOnlyList<string> LeaderEntries,
    IReadOnlyList<string> FollowerEntries,
    IReadOnlyList<string> CoupleEntries)
{
    public static HeatReviewViewModel From(ParserHeatReview heat) =>
        new(heat.Name, heat.Kind, heat.LeaderEntries, heat.FollowerEntries, heat.CoupleEntries);
}

public sealed record RolePromptViewModel(string HeatName, string HeatId, IReadOnlyList<string> Entries);
