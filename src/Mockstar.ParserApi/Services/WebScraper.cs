using AngleSharp.Html.Parser;

namespace Mockstar.Services;

public sealed class WebScraper
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WebScraper(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> ScrapeTextAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("A valid absolute URL is required.", nameof(url));
        }

        var client = _httpClientFactory.CreateClient();
        var html = await client.GetStringAsync(uri, cancellationToken);

        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(html, cancellationToken);
        return document.Body?.TextContent.Trim() ?? document.DocumentElement.TextContent.Trim();
    }
}
