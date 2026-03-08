using System.Net.Http.Json;
using System.Text.Json;
using Mockstar.ParserApi.Contracts;

namespace Mockstar.Web.Services.Imports;

public sealed class ParserApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> HandledErrorCodes =
    [
        "validation_failed",
        "parse_failed",
        "upstream_fetch_failed"
    ];

    private readonly HttpClient _httpClient;

    public ParserApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<ParserApiCallResult> ParseTextAsync(string text, CancellationToken cancellationToken = default) =>
        SendAsync("/api/parser/text", new ParseRosterTextRequest(text), cancellationToken);

    public Task<ParserApiCallResult> ParseUrlAsync(string url, CancellationToken cancellationToken = default) =>
        SendAsync("/api/parser/url", new ParseRosterUrlRequest(url), cancellationToken);

    private async Task<ParserApiCallResult> SendAsync<TRequest>(string path, TRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(path, request, JsonOptions, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadFromJsonAsync<ParserImportResponse>(JsonOptions, cancellationToken);
                return payload is null
                    ? ParserApiCallResult.Unavailable("Parsing service is temporarily unavailable.")
                    : ParserApiCallResult.Success(payload);
            }

            var problem = await ReadProblemAsync(response, cancellationToken);
            if (problem is { ErrorCode: not null, Detail: not null } && HandledErrorCodes.Contains(problem.ErrorCode))
            {
                return ParserApiCallResult.Failure(problem.Detail);
            }

            return ParserApiCallResult.Unavailable("Parsing service is temporarily unavailable.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return ParserApiCallResult.Unavailable("Parsing service is temporarily unavailable.");
        }
    }

    private static async Task<ParserProblem?> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;

            var detail = root.TryGetProperty("detail", out var detailElement) ? detailElement.GetString() : null;
            var errorCode = root.TryGetProperty("errorCode", out var errorCodeElement) ? errorCodeElement.GetString() : null;
            return new ParserProblem(detail, errorCode);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record ParserProblem(string? Detail, string? ErrorCode);
}

public sealed record ParserApiCallResult(ParserImportResponse? Response, string? ErrorMessage, bool IsSuccess)
{
    public static ParserApiCallResult Success(ParserImportResponse response) => new(response, null, true);

    public static ParserApiCallResult Failure(string message) => new(null, message, false);

    public static ParserApiCallResult Unavailable(string message) => new(null, message, false);
}
