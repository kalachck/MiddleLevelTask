using System.Net.Http.Json;
using DataIngestor.Domain.Abstractions;
using DataIngestor.Domain.Models;
using DataIngestor.Infrastructure.Configurations.Providers;
using Microsoft.Extensions.Logging;

namespace DataIngestor.Infrastructure;

public class WeakApiClient : IWeakApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeakApiClient> _logger;
    private readonly IWeakApiConfigProvider _configProvider;

    public WeakApiClient(
        HttpClient httpClient,
        ILogger<WeakApiClient> logger,
        IWeakApiConfigProvider configProvider)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configProvider = configProvider;
    }
    
    public async Task<IEnumerable<SensorReading>> FetchReadingsAsync(CancellationToken ct = default)
    {
        try
        {
            var headers = _configProvider.GetHeaders();
            
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(_configProvider.GetMetersUrl()),
            };
            headers.ToList().ForEach(h => request.Headers.Add(h.Key, h.Value));
            
            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
        
            return await response.Content.ReadFromJsonAsync<IEnumerable<SensorReading>>(cancellationToken: ct) 
                   ?? Enumerable.Empty<SensorReading>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Couldn't get the data after all attempts: {Message}", ex.Message);
            return Enumerable.Empty<SensorReading>();
        }
    }
}
