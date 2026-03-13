using Microsoft.EntityFrameworkCore;
using Mockstar.Web.Persistence.Entities;
using Mockstar.Web.Persistence.Mapping;
using Mockstar.Scoring;

namespace Mockstar.Web.Persistence;

public class HeatRepository : IHeatRepository
{
    private readonly HeatDbContext _context;

    public HeatRepository(HeatDbContext context)
    {
        _context = context;
    }

    public async Task SaveAsync(EventRecord eventRecord, CancellationToken ct = default)
    {
        var existingEvent = await _context.Events
            .Include(e => e.Heats)
            .FirstOrDefaultAsync(e => e.Id == eventRecord.Id, ct);

        if (existingEvent is not null)
        {
            // Update existing - remove old heats, add new ones
            _context.Heats.RemoveRange(existingEvent.Heats);
            existingEvent.Name = eventRecord.Name;
            existingEvent.UpdatedAt = DateTimeOffset.UtcNow;

            foreach (var heat in eventRecord.Heats)
            {
                var heatEntity = HeatMapper.ToEntity(heat, eventRecord.Id);
                existingEvent.Heats.Add(heatEntity);
            }
        }
        else
        {
            // Create new event with heats
            var eventEntity = new EventEntity
            {
                Id = eventRecord.Id,
                Name = eventRecord.Name,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            foreach (var heat in eventRecord.Heats)
            {
                var heatEntity = HeatMapper.ToEntity(heat, eventRecord.Id);
                eventEntity.Heats.Add(heatEntity);
            }

            _context.Events.Add(eventEntity);
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task<EventRecord?> LoadAsync(string eventId, CancellationToken ct = default)
    {
        var eventEntity = await _context.Events
            .Include(e => e.Heats)
            .FirstOrDefaultAsync(e => e.Id == eventId, ct);

        if (eventEntity is null)
            return null;

        var heats = eventEntity.Heats
            .Select(HeatMapper.ToDomain)
            .ToList();

        return new EventRecord(eventEntity.Id, eventEntity.Name, heats);
    }

    public async Task DeleteAsync(string eventId, CancellationToken ct = default)
    {
        var eventEntity = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId, ct);

        if (eventEntity is not null)
        {
            _context.Events.Remove(eventEntity);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<string>> ListEventIdsAsync(CancellationToken ct = default)
    {
        return await _context.Events
            .Select(e => e.Id)
            .ToListAsync(ct);
    }
}
