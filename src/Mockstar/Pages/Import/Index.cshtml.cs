using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mockstar.Domain;
using Mockstar.Models;
using Mockstar.Services;
using Mockstar.Services.Rosters;

namespace Mockstar.Pages.Import;

public sealed class IndexModel : PageModel
{
    private readonly RosterNormalizer _normalizer;
    private readonly RosterParser _parser;
    private readonly WebScraper _webScraper;

    public IndexModel(RosterParser parser, RosterNormalizer normalizer, WebScraper webScraper)
    {
        _parser = parser;
        _normalizer = normalizer;
        _webScraper = webScraper;
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

        try
        {
            var text = await _webScraper.ScrapeTextAsync(url, cancellationToken);
            return BuildReviewResult(text, ImportSourceKind.Web);
        }
        catch (Exception exception) when (exception is ArgumentException or HttpRequestException or InvalidOperationException)
        {
            return Partial("_ImportReview", ImportReviewViewModel.WithError(exception.Message));
        }
    }

    public IActionResult OnPostText(string extractedText)
    {
        if (string.IsNullOrWhiteSpace(extractedText))
        {
            return Partial("_ImportReview", ImportReviewViewModel.WithError("OCR did not produce any roster text."));
        }

        try
        {
            return BuildReviewResult(extractedText, ImportSourceKind.Image);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return Partial("_ImportReview", ImportReviewViewModel.WithError(exception.Message));
        }
    }

    private PartialViewResult BuildReviewResult(string sourceText, ImportSourceKind sourceKind)
    {
        var parsed = _parser.Parse(sourceText);
        var normalized = _normalizer.Normalize(parsed, sourceKind);
        var reviewModel = ImportReviewViewModel.From(parsed, normalized, sourceText);
        return Partial("_ImportReview", reviewModel);
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

    public static ImportReviewViewModel From(ParsedRosterDocument parsed, NormalizedRoster normalized, string sourceText)
    {
        var heats = normalized.EventRecord.Heats.Select(HeatReviewViewModel.From).ToArray();
        var prompts = normalized.RoleAssignmentPrompts
            .Select(prompt => new RolePromptViewModel(
                prompt.HeatName,
                normalized.EventRecord.Heats.First(heat => heat.Name == prompt.HeatName).Id,
                prompt.Entries.Select(entry => entry.Display).ToArray()))
            .ToArray();
        var activationPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ToClientEventRecord(parsed, normalized.EventRecord), ActivationJsonOptions)));

        return new ImportReviewViewModel(null, sourceText, normalized.EventRecord.Name, heats, prompts, activationPayload);
    }

    private static ClientEventRecord ToClientEventRecord(ParsedRosterDocument parsed, EventRecord eventRecord)
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
                var importSource = new ClientImportSource(
                    heat.ImportSource.Kind.ToString().ToLowerInvariant(),
                    heat.ImportSource.Timestamp.ToString("O"));

                return heat switch
                {
                    JackAndJillFinalHeat finalHeat => new ClientHeat(
                        finalHeat.Id,
                        finalHeat.Name,
                        type,
                        phase,
                        finalHeat.LeaderEntries.Select(entry => new ClientBibEntry(entry.Id, entry.Bib, entry.Display)).ToArray(),
                        finalHeat.FollowerEntries.Select(entry => new ClientBibEntry(entry.Id, entry.Bib, entry.Display)).ToArray(),
                        Array.Empty<ClientCoupleEntry>(),
                        finalHeat.Pairings.Select(pairing => new ClientPairing(pairing.LeaderBib, pairing.FollowerBib)).ToArray(),
                        parsedHeat.AmbiguousBibs.Select(bib => new ClientBibEntry($"bib-{bib}", bib, bib.ToString())).ToArray(),
                        importSource),
                    JackAndJillPrelimHeat prelimHeat => new ClientHeat(
                        prelimHeat.Id,
                        prelimHeat.Name,
                        type,
                        phase,
                        prelimHeat.LeaderEntries.Select(entry => new ClientBibEntry(entry.Id, entry.Bib, entry.Display)).ToArray(),
                        prelimHeat.FollowerEntries.Select(entry => new ClientBibEntry(entry.Id, entry.Bib, entry.Display)).ToArray(),
                        Array.Empty<ClientCoupleEntry>(),
                        Array.Empty<ClientPairing>(),
                        parsedHeat.AmbiguousBibs.Select(bib => new ClientBibEntry($"bib-{bib}", bib, bib.ToString())).ToArray(),
                        importSource),
                    StrictlyHeat strictlyHeat => new ClientHeat(
                        strictlyHeat.Id,
                        strictlyHeat.Name,
                        type,
                        phase,
                        Array.Empty<ClientBibEntry>(),
                        Array.Empty<ClientBibEntry>(),
                        strictlyHeat.CoupleEntries.Select(entry => new ClientCoupleEntry(entry.Id, entry.LeaderBib, entry.FollowerBib, entry.Display)).ToArray(),
                        Array.Empty<ClientPairing>(),
                        Array.Empty<ClientBibEntry>(),
                        importSource),
                    _ => throw new InvalidOperationException($"Unsupported heat type {heat.GetType().Name}.")
                };
            })
            .ToArray();

        return new ClientEventRecord(eventRecord.Id, eventRecord.Name, heats);
    }
}

public sealed record HeatReviewViewModel(
    string Name,
    string Kind,
    IReadOnlyList<string> LeaderEntries,
    IReadOnlyList<string> FollowerEntries,
    IReadOnlyList<string> CoupleEntries)
{
    public static HeatReviewViewModel From(Heat heat) =>
        heat switch
        {
            JackAndJillFinalHeat finalHeat => new HeatReviewViewModel(
                finalHeat.Name,
                "Jack and Jill Final",
                finalHeat.LeaderEntries.Select(entry => entry.Display).ToArray(),
                finalHeat.FollowerEntries.Select(entry => entry.Display).ToArray(),
                Array.Empty<string>()),
            JackAndJillPrelimHeat prelimHeat => new HeatReviewViewModel(
                prelimHeat.Name,
                $"Jack and Jill {prelimHeat.Phase}",
                prelimHeat.LeaderEntries.Select(entry => entry.Display).ToArray(),
                prelimHeat.FollowerEntries.Select(entry => entry.Display).ToArray(),
                Array.Empty<string>()),
            StrictlyHeat strictlyHeat => new HeatReviewViewModel(
                strictlyHeat.Name,
                $"Strictly {strictlyHeat.Phase}",
                Array.Empty<string>(),
                Array.Empty<string>(),
                strictlyHeat.CoupleEntries.Select(entry => entry.Display).ToArray()),
            _ => throw new InvalidOperationException($"Unsupported heat type {heat.GetType().Name}.")
        };
}

public sealed record RolePromptViewModel(string HeatName, string HeatId, IReadOnlyList<string> Entries);
