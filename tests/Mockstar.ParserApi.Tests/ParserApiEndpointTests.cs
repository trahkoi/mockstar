using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Mockstar.Parser.Contracts;

namespace Mockstar.Tests;

public sealed class ParserApiEndpointTests
{
    [Fact]
    public async Task TextEndpointReturnsImportReviewData()
    {
        await using var factory = new WebApplicationFactory<Mockstar.ParserApi.Program>();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/parser/text", new ParseRosterTextRequest(
            """
            Liberty Swing
            Advanced Jack and Jill Semis
            Heat 1
            Leaders
            101 102 103
            Followers
            201 202 203
            """));

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ParserImportResponse>();

        Assert.NotNull(payload);
        Assert.Equal("Liberty Swing", payload.EventName);
        Assert.Single(payload.Heats);
        Assert.Equal("Jack and Jill Semifinal", payload.Heats[0].Kind);
        Assert.Equal(new[] { "101", "102", "103" }, payload.Heats[0].LeaderEntries);
    }

    [Fact]
    public async Task TextEndpointReturnsValidationProblemForEmptyText()
    {
        await using var factory = new WebApplicationFactory<Mockstar.ParserApi.Program>();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/parser/text", new ParseRosterTextRequest(" "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Roster text is required.", document.RootElement.GetProperty("detail").GetString());
        Assert.Equal("validation_failed", document.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task UrlEndpointReturnsImportReviewData()
    {
        await using var factory = CreateFactory(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                <html>
                  <body>
                    Liberty Swing
                    Advanced Jack and Jill Final
                    Heat 1
                    Leaders
                    9 11 15
                  </body>
                </html>
                """)
        });
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/parser/url", new ParseRosterUrlRequest("https://example.com/roster"));

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ParserImportResponse>();

        Assert.NotNull(payload);
        Assert.Contains("Liberty Swing", payload.SourceText);
        Assert.Equal("jack-and-jill-final", payload.EventRecord.Heats[0].Type);
        Assert.Equal(new[] { 9, 11, 15 }, payload.EventRecord.Heats[0].LeaderEntries.Select(entry => entry.Bib));
    }

    [Fact]
    public async Task UrlEndpointReturnsUpstreamFetchProblem()
    {
        await using var factory = CreateFactory(_ => throw new HttpRequestException("upstream unavailable"));
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/parser/url", new ParseRosterUrlRequest("https://example.com/roster"));

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("upstream unavailable", document.RootElement.GetProperty("detail").GetString());
        Assert.Equal("upstream_fetch_failed", document.RootElement.GetProperty("errorCode").GetString());
    }

    private static WebApplicationFactory<Mockstar.ParserApi.Program> CreateFactory(Func<HttpRequestMessage, HttpResponseMessage> handler) =>
        new WebApplicationFactory<Mockstar.ParserApi.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(new HttpClient(new StubHttpMessageHandler(handler))));
                });
            });

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public StubHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name) => _client;
    }

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
