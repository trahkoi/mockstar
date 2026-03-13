using Mockstar.Scoring;

namespace Mockstar.Web.Persistence;

public interface IHeatRepository
{
    Task SaveAsync(EventRecord eventRecord, CancellationToken ct = default);
    Task<EventRecord?> LoadAsync(string eventId, CancellationToken ct = default);
    Task DeleteAsync(string eventId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListEventIdsAsync(CancellationToken ct = default);
}
