using DataIngestor.Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DataIngestor.Application;

public class IngestionWorker : BackgroundService
{
    private readonly ILogger<IngestionWorker> _logger;
    private readonly IWeakApiClient _weakApiClient;
    private readonly IMessagePublisher _messagePublisher;
    
    public IngestionWorker(
        IServiceProvider serviceProvider,
        ILogger<IngestionWorker> logger)
    {
        _logger = logger;
        _weakApiClient = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IWeakApiClient>();
        _messagePublisher = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IMessagePublisher>();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        do
        {
            try
            {
                var data = await _weakApiClient.FetchReadingsAsync(stoppingToken);
                foreach (var reading in data)
                {
                    await _messagePublisher.PublishAsync(reading, stoppingToken);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occured while retrieving readings");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
