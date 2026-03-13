using Microsoft.EntityFrameworkCore;
using Mockstar.ParserApi.Persistence;
using Mockstar.Scoring;

namespace Mockstar.ParserApi.Tests;

public sealed class HeatRepositoryTests : IDisposable
{
    private readonly HeatDbContext _context;
    private readonly HeatRepository _repository;

    public HeatRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<HeatDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new HeatDbContext(options);
        _repository = new HeatRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task SaveAndLoadRoundTripsEventRecord()
    {
        var importSource = new ImportSource(ImportSourceKind.Web, DateTimeOffset.UtcNow);
        var heat = new JackAndJillPrelimHeat(
            "heat-1",
            "Heat 1",
            RoundPhase.Prelim,
            [new BibEntry("leader-1", 101), new BibEntry("leader-2", 102)],
            [new BibEntry("follower-1", 201), new BibEntry("follower-2", 202)],
            importSource);

        var eventRecord = new EventRecord("test-event-123", "Test Event", [heat]);

        await _repository.SaveAsync(eventRecord);
        var loaded = await _repository.LoadAsync("test-event-123");

        Assert.NotNull(loaded);
        Assert.Equal("test-event-123", loaded.Id);
        Assert.Equal("Test Event", loaded.Name);
        Assert.Single(loaded.Heats);

        var loadedHeat = Assert.IsType<JackAndJillPrelimHeat>(loaded.Heats[0]);
        Assert.Equal("heat-1", loadedHeat.Id);
        Assert.Equal(2, loadedHeat.LeaderEntries.Count);
        Assert.Equal(101, loadedHeat.LeaderEntries[0].Bib);
    }

    [Fact]
    public async Task SaveReplacesExistingHeats()
    {
        var importSource = new ImportSource(ImportSourceKind.Web, DateTimeOffset.UtcNow);
        var heat1 = new JackAndJillPrelimHeat("heat-1", "Heat 1", RoundPhase.Prelim, [], [], importSource);
        var event1 = new EventRecord("event-1", "Event V1", [heat1]);

        await _repository.SaveAsync(event1);

        var heat2 = new JackAndJillPrelimHeat("heat-2", "Heat 2", RoundPhase.Semifinal, [], [], importSource);
        var event2 = new EventRecord("event-1", "Event V2", [heat2]);

        await _repository.SaveAsync(event2);

        var loaded = await _repository.LoadAsync("event-1");

        Assert.NotNull(loaded);
        Assert.Equal("Event V2", loaded.Name);
        Assert.Single(loaded.Heats);
        Assert.Equal("heat-2", loaded.Heats[0].Id);
    }

    [Fact]
    public async Task LoadReturnsNullForUnknownEvent()
    {
        var loaded = await _repository.LoadAsync("nonexistent");
        Assert.Null(loaded);
    }

    [Fact]
    public async Task DeleteRemovesEvent()
    {
        var importSource = new ImportSource(ImportSourceKind.Web, DateTimeOffset.UtcNow);
        var heat = new JackAndJillPrelimHeat("heat-1", "Heat 1", RoundPhase.Prelim, [], [], importSource);
        var eventRecord = new EventRecord("to-delete", "Event", [heat]);

        await _repository.SaveAsync(eventRecord);
        await _repository.DeleteAsync("to-delete");

        var loaded = await _repository.LoadAsync("to-delete");
        Assert.Null(loaded);
    }

    [Fact]
    public async Task ListEventIdsReturnsAllStoredEvents()
    {
        var importSource = new ImportSource(ImportSourceKind.Web, DateTimeOffset.UtcNow);

        await _repository.SaveAsync(new EventRecord("event-a", "A", []));
        await _repository.SaveAsync(new EventRecord("event-b", "B", []));
        await _repository.SaveAsync(new EventRecord("event-c", "C", []));

        var ids = await _repository.ListEventIdsAsync();

        Assert.Equal(3, ids.Count);
        Assert.Contains("event-a", ids);
        Assert.Contains("event-b", ids);
        Assert.Contains("event-c", ids);
    }

    [Fact]
    public async Task SaveAndLoadFinalHeatWithPairings()
    {
        var importSource = new ImportSource(ImportSourceKind.Web, DateTimeOffset.UtcNow);
        var heat = new JackAndJillFinalHeat(
            "final-1",
            "Final Heat",
            [new BibEntry("l1", 101)],
            [new BibEntry("f1", 201)],
            [new Pairing(101, 201)],
            importSource);

        var eventRecord = new EventRecord("final-event", "Final Event", [heat]);

        await _repository.SaveAsync(eventRecord);
        var loaded = await _repository.LoadAsync("final-event");

        Assert.NotNull(loaded);
        var loadedHeat = Assert.IsType<JackAndJillFinalHeat>(loaded.Heats[0]);
        Assert.Single(loadedHeat.Pairings);
        Assert.Equal(101, loadedHeat.Pairings[0].LeaderBib);
        Assert.Equal(201, loadedHeat.Pairings[0].FollowerBib);
    }

    [Fact]
    public async Task SaveAndLoadStrictlyHeat()
    {
        var importSource = new ImportSource(ImportSourceKind.Web, DateTimeOffset.UtcNow);
        var heat = new StrictlyHeat(
            "strictly-1",
            "Strictly Heat",
            RoundPhase.Final,
            [new CoupleEntry("c1", 101, 201)],
            importSource);

        var eventRecord = new EventRecord("strictly-event", "Strictly Event", [heat]);

        await _repository.SaveAsync(eventRecord);
        var loaded = await _repository.LoadAsync("strictly-event");

        Assert.NotNull(loaded);
        var loadedHeat = Assert.IsType<StrictlyHeat>(loaded.Heats[0]);
        Assert.Single(loadedHeat.CoupleEntries);
        Assert.Equal(101, loadedHeat.CoupleEntries[0].LeaderBib);
        Assert.Equal(201, loadedHeat.CoupleEntries[0].FollowerBib);
    }
}
