using System.Text.Json;
using Mockstar.Web.Persistence.Entities;
using Mockstar.Scoring;

namespace Mockstar.Web.Persistence.Mapping;

public static class HeatMapper
{
    public static HeatEntity ToEntity(Heat heat, string eventId)
    {
        var entity = new HeatEntity
        {
            HeatId = heat.Id,
            Name = heat.Name,
            Phase = heat.Phase,
            DivisionKind = heat.DivisionKind,
            HeatType = heat.GetType().Name,
            ImportSourceKind = heat.ImportSource.Kind,
            ImportTimestamp = heat.ImportSource.Timestamp,
            EventId = eventId
        };

        entity.EntriesJson = heat switch
        {
            JackAndJillPrelimHeat prelim => JsonSerializer.Serialize(new PrelimEntriesDto(
                prelim.LeaderEntries.Select(e => new BibEntryDto(e.Id, e.Bib)).ToList(),
                prelim.FollowerEntries.Select(e => new BibEntryDto(e.Id, e.Bib)).ToList())),

            JackAndJillFinalHeat final => JsonSerializer.Serialize(new FinalEntriesDto(
                final.LeaderEntries.Select(e => new BibEntryDto(e.Id, e.Bib)).ToList(),
                final.FollowerEntries.Select(e => new BibEntryDto(e.Id, e.Bib)).ToList(),
                final.Pairings.Select(p => new PairingDto(p.LeaderBib, p.FollowerBib)).ToList())),

            StrictlyHeat strictly => JsonSerializer.Serialize(new StrictlyEntriesDto(
                strictly.CoupleEntries.Select(e => new CoupleEntryDto(e.Id, e.LeaderBib, e.FollowerBib)).ToList())),

            _ => "{}"
        };

        return entity;
    }

    public static Heat ToDomain(HeatEntity entity)
    {
        var importSource = new ImportSource(entity.ImportSourceKind, entity.ImportTimestamp);

        return entity.HeatType switch
        {
            nameof(JackAndJillPrelimHeat) => ToPrelimHeat(entity, importSource),
            nameof(JackAndJillFinalHeat) => ToFinalHeat(entity, importSource),
            nameof(StrictlyHeat) => ToStrictlyHeat(entity, importSource),
            _ => throw new InvalidOperationException($"Unknown heat type: {entity.HeatType}")
        };
    }

    private static JackAndJillPrelimHeat ToPrelimHeat(HeatEntity entity, ImportSource importSource)
    {
        var dto = JsonSerializer.Deserialize<PrelimEntriesDto>(entity.EntriesJson)
            ?? throw new InvalidOperationException("Failed to deserialize prelim entries");

        return new JackAndJillPrelimHeat(
            entity.HeatId,
            entity.Name,
            entity.Phase,
            dto.Leaders.Select(e => new BibEntry(e.Id, e.Bib)).ToList(),
            dto.Followers.Select(e => new BibEntry(e.Id, e.Bib)).ToList(),
            importSource);
    }

    private static JackAndJillFinalHeat ToFinalHeat(HeatEntity entity, ImportSource importSource)
    {
        var dto = JsonSerializer.Deserialize<FinalEntriesDto>(entity.EntriesJson)
            ?? throw new InvalidOperationException("Failed to deserialize final entries");

        return new JackAndJillFinalHeat(
            entity.HeatId,
            entity.Name,
            dto.Leaders.Select(e => new BibEntry(e.Id, e.Bib)).ToList(),
            dto.Followers.Select(e => new BibEntry(e.Id, e.Bib)).ToList(),
            dto.Pairings.Select(p => new Pairing(p.LeaderBib, p.FollowerBib)).ToList(),
            importSource);
    }

    private static StrictlyHeat ToStrictlyHeat(HeatEntity entity, ImportSource importSource)
    {
        var dto = JsonSerializer.Deserialize<StrictlyEntriesDto>(entity.EntriesJson)
            ?? throw new InvalidOperationException("Failed to deserialize strictly entries");

        return new StrictlyHeat(
            entity.HeatId,
            entity.Name,
            entity.Phase,
            dto.Couples.Select(e => new CoupleEntry(e.Id, e.LeaderBib, e.FollowerBib)).ToList(),
            importSource);
    }

    // DTOs for JSON serialization
    private record BibEntryDto(string Id, int Bib);
    private record CoupleEntryDto(string Id, int LeaderBib, int? FollowerBib);
    private record PairingDto(int LeaderBib, int FollowerBib);
    private record PrelimEntriesDto(List<BibEntryDto> Leaders, List<BibEntryDto> Followers);
    private record FinalEntriesDto(List<BibEntryDto> Leaders, List<BibEntryDto> Followers, List<PairingDto> Pairings);
    private record StrictlyEntriesDto(List<CoupleEntryDto> Couples);
}
