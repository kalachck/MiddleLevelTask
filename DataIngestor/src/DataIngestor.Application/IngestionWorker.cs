using DataIngestor.Application.Configurations.Models;
using DataIngestor.Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataIngestor.Application;

public class IngestionWorker : BackgroundService
{
    private readonly ILogger<IngestionWorker> _logger;
    private readonly IWeakApiClient _weakApiClient;
    private readonly IMessagePublisher _messagePublisher;
    private readonly TimeSpan _pollingInterval;

    public IngestionWorker(
        IServiceProvider serviceProvider,
        ILogger<IngestionWorker> logger,
        IOptions<IngestionOptions> options)
    {
        _logger = logger;
        _pollingInterval = options.Value.Interval;
        _weakApiClient = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IWeakApiClient>();
        _messagePublisher = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IMessagePublisher>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_pollingInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                $"{IngestionOptions.SectionName}:{nameof(IngestionOptions.Interval)} must be greater than zero.");
        }

        _logger.LogInformation("Ingestion polling interval: {Interval}", _pollingInterval);
        using var timer = new PeriodicTimer(_pollingInterval);

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
