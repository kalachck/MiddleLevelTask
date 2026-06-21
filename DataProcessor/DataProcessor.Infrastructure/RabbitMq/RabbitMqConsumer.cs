using System.Text;
using DataProcessor.Application.Interfaces.Services;
using DataProcessor.Application.Metrics;
using DataProcessor.Infrastructure.RabbitMq.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DataProcessor.Infrastructure.RabbitMq;

public class RabbitMqConsumer<TService> : BackgroundService
    where TService : IReadingProcessingService
{
    private readonly string _queueName;
    private readonly IRabbitMqChannelProvider _channelProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqConsumer<TService>> _logger;

    private readonly DataProcessorMetrics _metrics;

    public RabbitMqConsumer(
        string queueName,
        IRabbitMqChannelProvider channelProvider,
        IServiceProvider serviceProvider,
        DataProcessorMetrics metrics,
        ILogger<RabbitMqConsumer<TService>> logger)
    {
        _queueName = queueName;
        _channelProvider = channelProvider;
        _serviceProvider = serviceProvider;
        _metrics = metrics;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var channel = await _channelProvider.GetChannel(ct);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            using var duration = _metrics.TrackProcessingDuration(_queueName);

            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                _logger.LogDebug(
                    "Received message on queue {QueueName} (delivery tag {DeliveryTag}, {ByteCount} bytes)",
                    _queueName,
                    ea.DeliveryTag,
                    body.Length);

                using var scope = _serviceProvider.CreateScope();
                var processingService = scope.ServiceProvider.GetRequiredKeyedService<IReadingProcessingService>(
                    typeof(TService).Name);

                await processingService.ProcessReading(message, ct);

                await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                _metrics.RecordProcessed(_queueName);

                _logger.LogDebug(
                    "Acknowledged message {DeliveryTag} on queue {QueueName}",
                    ea.DeliveryTag,
                    _queueName);
            }
            catch (Exception ex)
            {
                _metrics.RecordFailed(_queueName);

                _logger.LogError(
                    ex,
                    "Failed to process message on queue {QueueName} (delivery tag {DeliveryTag}); message will be requeued",
                    _queueName,
                    ea.DeliveryTag);

                await channel.BasicNackAsync(ea.DeliveryTag, false, true, ct);
            }
        };

        await channel.BasicConsumeAsync(
            queue: _queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: ct);

        _logger.LogInformation(
            "RabbitMQ consumer for {ProcessorType} is listening on queue {QueueName}",
            typeof(TService).Name,
            _queueName);

        await Task.Delay(Timeout.Infinite, ct);
    }
}
