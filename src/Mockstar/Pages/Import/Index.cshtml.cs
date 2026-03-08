using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mockstar.ParserApi.Contracts;
using Mockstar.Services.Imports;

namespace Mockstar.Pages.Import;

public sealed class IndexModel : PageModel
{
    private readonly ParserApiClient _parserApiClient;

    public IndexModel(ParserApiClient parserApiClient)
    {
        _parserApiClient = parserApiClient;
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
