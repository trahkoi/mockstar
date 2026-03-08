using Mockstar.ParserApi.Contracts;
using Mockstar.Scoring;
using Mockstar.ParserApi.Services.Rosters;
using Mockstar.ParserApi.Services;

namespace Mockstar.ParserApi.Services.Rosters;

public sealed class ParserImportService
{
    private readonly RosterParser _parser;
    private readonly RosterNormalizer _normalizer;
    private readonly WebScraper _webScraper;

    public ParserImportService(RosterParser parser, RosterNormalizer normalizer, WebScraper webScraper)
    {
        _parser = parser;
        _normalizer = normalizer;
        _webScraper = webScraper;
    }

    public Task<ParserImportResponse> ParseTextAsync(string sourceText, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Parse(sourceText, ImportSourceKind.Image));
    }

    public async Task<ParserImportResponse> ParseUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        var sourceText = await _webScraper.ScrapeTextAsync(url, cancellationToken);
        return Parse(sourceText, ImportSourceKind.Web);
    }

    private ParserImportResponse Parse(string sourceText, ImportSourceKind sourceKind)
    {
        var parsed = _parser.Parse(sourceText);
        var normalized = _normalizer.Normalize(parsed, sourceKind);
        return ParserImportResponseFactory.Create(parsed, normalized, sourceText);
    }
}
