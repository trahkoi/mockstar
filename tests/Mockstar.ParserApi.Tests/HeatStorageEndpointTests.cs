using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Mockstar.ParserApi.Contracts;

namespace Mockstar.ParserApi.Tests;

public sealed class HeatStorageEndpointTests
{
    [Fact]
    public async Task SaveAndLoadHeatsRoundTrip()
    {
        await using var factory = new WebApplicationFactory<Mockstar.ParserApi.Program>();
        using var client = factory.CreateClient();

        var eventRecord = new ParserEventRecord(
            "test-event-api",
            "Test Event",
            [
                new ParserHeat(
                    "heat-1",
                    "Heat 1",
                    "JackAndJillPrelimHeat",
                    "Prelim",
                    [new ParserBibEntry("l1", 101, "101")],
                    [new ParserBibEntry("f1", 201, "201")],
                    [],
                    [],
                    [],
                    new ParserImportSource("Web", DateTimeOffset.UtcNow.ToString("O")))
            ]);

        // Save
        using var saveResponse = await client.PostAsJsonAsync(
            "/api/heats/test-event-api",
            new SaveHeatsRequest(eventRecord));
        saveResponse.EnsureSuccessStatusCode();

        // Load
        using var loadResponse = await client.GetAsync("/api/heats/test-event-api");
        loadResponse.EnsureSuccessStatusCode();

        var loaded = await loadResponse.Content.ReadFromJsonAsync<LoadHeatsResponse>();
        Assert.NotNull(loaded?.EventRecord);
        Assert.Equal("test-event-api", loaded.EventRecord.Id);
        Assert.Single(loaded.EventRecord.Heats);
        Assert.Equal(101, loaded.EventRecord.Heats[0].LeaderEntries[0].Bib);
    }

    [Fact]
    public async Task LoadReturnsNullForUnknownEvent()
    {
        await using var factory = new WebApplicationFactory<Mockstar.ParserApi.Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/heats/nonexistent-event");
        response.EnsureSuccessStatusCode();

        var loaded = await response.Content.ReadFromJsonAsync<LoadHeatsResponse>();
        Assert.Null(loaded?.EventRecord);
    }

    [Fact]
    public async Task DeleteRemovesStoredHeats()
    {
        await using var factory = new WebApplicationFactory<Mockstar.ParserApi.Program>();
        using var client = factory.CreateClient();

        var eventRecord = new ParserEventRecord("to-delete-api", "Event", []);

        await client.PostAsJsonAsync("/api/heats/to-delete-api", new SaveHeatsRequest(eventRecord));
        await client.DeleteAsync("/api/heats/to-delete-api");

        using var response = await client.GetAsync("/api/heats/to-delete-api");
        var loaded = await response.Content.ReadFromJsonAsync<LoadHeatsResponse>();
        Assert.Null(loaded?.EventRecord);
    }

    [Fact]
    public async Task ListEventsReturnsStoredEventIds()
    {
        await using var factory = new WebApplicationFactory<Mockstar.ParserApi.Program>();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/heats/event-x", new SaveHeatsRequest(new ParserEventRecord("event-x", "X", [])));
        await client.PostAsJsonAsync("/api/heats/event-y", new SaveHeatsRequest(new ParserEventRecord("event-y", "Y", [])));

        using var response = await client.GetAsync("/api/heats");
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<ListEventsResponse>();
        Assert.NotNull(list);
        Assert.Contains("event-x", list.EventIds);
        Assert.Contains("event-y", list.EventIds);
    }
}
