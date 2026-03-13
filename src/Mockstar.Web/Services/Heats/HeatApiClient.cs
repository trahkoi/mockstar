using Mockstar.ParserApi.Contracts;
using Mockstar.Web.Persistence;
using Mockstar.Web.Persistence.Mapping;

namespace Mockstar.Web.Services.Heats;

public sealed class HeatApiClient
{
    private readonly IHeatRepository _repository;

    public HeatApiClient(IHeatRepository repository)
    {
        _repository = repository;
    }

    public async Task<(ParserHeat? Heat, string? EventId)> GetHeatByIdAsync(string heatId, CancellationToken cancellationToken = default)
    {
        var eventIds = await _repository.ListEventIdsAsync(cancellationToken);

        foreach (var eventId in eventIds)
        {
            var eventRecord = await _repository.LoadAsync(eventId, cancellationToken);
            if (eventRecord is null)
            {
                continue;
            }

            var contract = ContractMapper.ToContract(eventRecord);
            var heat = contract.Heats.FirstOrDefault(h => h.Id == heatId);
            if (heat is not null)
            {
                return (heat, eventId);
            }
        }

        return (null, null);
    }

    public async Task<bool> SaveEventAsync(string eventId, ParserEventRecord eventRecord, CancellationToken cancellationToken = default)
    {
        var domainEvent = ContractMapper.ToDomain(eventRecord);
        var eventToSave = domainEvent with { Id = eventId };
        await _repository.SaveAsync(eventToSave, cancellationToken);
        return true;
    }

    public async Task<ParserEventRecord?> GetEventAsync(string eventId, CancellationToken cancellationToken = default)
    {
        var eventRecord = await _repository.LoadAsync(eventId, cancellationToken);
        return eventRecord is not null ? ContractMapper.ToContract(eventRecord) : null;
    }

    public async Task<IReadOnlyList<string>> ListEventIdsAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.ListEventIdsAsync(cancellationToken);
    }
}
