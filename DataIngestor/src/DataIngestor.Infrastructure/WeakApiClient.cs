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
        var metersUrl = _configProvider.GetMetersUrl();

        try
        {
            _logger.LogDebug("Requesting sensor readings from {MetersUrl}", metersUrl);

            var headers = _configProvider.GetHeaders();

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(metersUrl),
            };
            headers.ToList().ForEach(h => request.Headers.Add(h.Key, h.Value));
            
            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var readings = await response.Content.ReadFromJsonAsync<IEnumerable<SensorReading>>(cancellationToken: ct)
                ?? Enumerable.Empty<SensorReading>();

            var list = readings.ToList();
            _logger.LogInformation(
                "Received {ReadingCount} readings from weak API ({StatusCode})",
                list.Count,
                (int)response.StatusCode);

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to fetch readings from weak API at {MetersUrl}",
                metersUrl);
            return Enumerable.Empty<SensorReading>();
        }
    }
}
