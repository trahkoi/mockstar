using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Mockstar.Pages.Import;
using Mockstar.ParserApi.Contracts;
using Mockstar.Services.Imports;

namespace Mockstar.Tests;

public sealed class ParserApiClientTests
{
    [Fact]
    public async Task ParserApiClientReturnsSuccessPayloadAndPreservesActivationShape()
    {
        var expected = CreateResponse();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        }))
        {
            BaseAddress = new Uri("http://localhost")
        };

        var client = new ParserApiClient(httpClient);

        var result = await client.ParseTextAsync("ocr text");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Response);
        Assert.Equal(expected.EventName, result.Response.EventName);

        var review = ImportReviewViewModel.From(result.Response);
        Assert.True(review.HasData);
        Assert.Equal(expected.EventName, review.EventName);

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(review.ActivationPayload!));
        using var document = JsonDocument.Parse(json);

        Assert.Equal(expected.EventRecord.Id, document.RootElement.GetProperty("id").GetString());
        Assert.Equal(expected.EventRecord.Heats[0].Type, document.RootElement.GetProperty("heats")[0].GetProperty("type").GetString());
        Assert.Equal(expected.EventRecord.Heats[0].ImportSource.Kind, document.RootElement.GetProperty("heats")[0].GetProperty("importSource").GetProperty("kind").GetString());
    }

    [Fact]
    public async Task ParserApiClientReturnsHandledParserFailuresToImportFlow()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = JsonContent.Create(new
            {
                title = "Parser request failed",
                detail = "No roster entries found in the provided text.",
                errorCode = "parse_failed"
            })
        }))
        {
            BaseAddress = new Uri("http://localhost")
        };

        var client = new ParserApiClient(httpClient);

        var result = await client.ParseTextAsync("bad input");

        Assert.False(result.IsSuccess);
        Assert.Equal("No roster entries found in the provided text.", result.ErrorMessage);
    }

    [Fact]
    public async Task ParserApiClientReturnsServiceUnavailableForUnexpectedFailures()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)))
        {
            BaseAddress = new Uri("http://localhost")
        };

        var client = new ParserApiClient(httpClient);

        var result = await client.ParseUrlAsync("https://example.com/roster");

        Assert.False(result.IsSuccess);
        Assert.Equal("Parsing service is temporarily unavailable.", result.ErrorMessage);
    }

    private static ParserImportResponse CreateResponse() =>
        new(
            "Liberty Swing\nAdvanced Jack and Jill Final",
            "Liberty Swing",
            [
                new ParserHeatReview(
                    "Advanced Jack and Jill Final Heat 1",
                    "Jack and Jill Final",
                    ["9", "11", "15"],
                    [],
                    [])
            ],
            [],
            new ParserEventRecord(
                "liberty-swing-20260308123045",
                "Liberty Swing",
                [
                    new ParserHeat(
                        "liberty-swing-20260308123045-heat-1",
                        "Advanced Jack and Jill Final Heat 1",
                        "jack-and-jill-final",
                        "final",
                        [new ParserBibEntry("bib-9", 9, "9"), new ParserBibEntry("bib-11", 11, "11"), new ParserBibEntry("bib-15", 15, "15")],
                        [],
                        [],
                        [],
                        [],
                        new ParserImportSource("image", "2026-03-08T12:30:45.0000000+00:00"))
                ]));

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_handler(request));
    }
}
