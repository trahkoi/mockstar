using System.Text.Json;
using Mockstar.ParserApi.Contracts;

namespace Mockstar.Web.Services.Heats;

public sealed class HeatApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public HeatApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(ParserHeat? Heat, string? EventId)> GetHeatByIdAsync(string heatId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all event IDs
            var eventIds = await ListEventIdsAsync(cancellationToken);

            // Search each event for the heat
            foreach (var eventId in eventIds)
            {
                var response = await _httpClient.GetFromJsonAsync<LoadHeatsResponse>(
                    $"/api/heats/{Uri.EscapeDataString(eventId)}",
                    JsonOptions,
                    cancellationToken);

                var heat = response?.EventRecord?.Heats.FirstOrDefault(h => h.Id == heatId);
                if (heat is not null)
                {
                    return (heat, eventId);
                }
            }

            return (null, null);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return (null, null);
        }
    }

    public async Task<bool> SaveEventAsync(string eventId, ParserEventRecord eventRecord, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new SaveHeatsRequest(eventRecord);
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/heats/{Uri.EscapeDataString(eventId)}",
                request,
                JsonOptions,
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }

    public async Task<ParserEventRecord?> GetEventAsync(string eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<LoadHeatsResponse>(
                $"/api/heats/{Uri.EscapeDataString(eventId)}",
                JsonOptions,
                cancellationToken);

            return response?.EventRecord;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<string>> ListEventIdsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ListEventsResponse>(
                "/api/heats",
                JsonOptions,
                cancellationToken);

            return response?.EventIds ?? [];
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return [];
        }
    }
}
