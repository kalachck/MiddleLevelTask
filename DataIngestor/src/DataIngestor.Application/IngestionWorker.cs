using DataIngestor.Application.Configurations.Models;
using DataIngestor.Application.Metrics;
using DataIngestor.Domain.Abstractions;
using DataIngestor.Domain.Models;
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
    private readonly IngestionMetrics _metrics;
    private readonly TimeSpan _pollingInterval;

    public IngestionWorker(
        IServiceProvider serviceProvider,
        ILogger<IngestionWorker> logger,
        IngestionMetrics metrics,
        IOptions<IngestionOptions> options)
    {
        _logger = logger;
        _metrics = metrics;
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

        _logger.LogInformation("Ingestion worker started; polling interval {Interval}", _pollingInterval);
        using var timer = new PeriodicTimer(_pollingInterval);

        do
        {
            using var duration = _metrics.TrackPollDuration();

            try
            {
                _logger.LogDebug("Starting ingestion poll cycle");

                var readings = (await _weakApiClient.FetchReadingsAsync(stoppingToken)).ToList();

                if (readings.Count == 0)
                {
                    _logger.LogDebug("No readings returned from weak API this cycle");
                    _metrics.RecordPollCycle();
                    continue;
                }

                _logger.LogInformation("Fetched {ReadingCount} readings from weak API", readings.Count);

                foreach (var group in readings.GroupBy(r => r.Type))
                {
                    _metrics.RecordReadingsFetched(group.Count(), sensorType: group.Key.ToString());
                }

                foreach (var reading in readings)
                {
                    await PublishReadingAsync(reading, stoppingToken);
                }

                _metrics.RecordPollCycle();

                _logger.LogInformation(
                    "Ingestion cycle completed: published {ReadingCount} readings",
                    readings.Count);
            }
            catch (Exception ex)
            {
                _metrics.RecordPollError();
                _logger.LogError(ex, "Ingestion poll cycle failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PublishReadingAsync(SensorReading reading, CancellationToken ct)
    {
        _logger.LogDebug(
            "Publishing {SensorType} reading for {Location}",
            reading.Type,
            reading.Name);

        await _messagePublisher.PublishAsync(reading, ct);
        _metrics.RecordReadingPublished(reading.Type.ToString());
    }
}
