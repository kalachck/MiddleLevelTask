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
    private readonly int _maxRetries;
    private readonly IRabbitMqChannelProvider _channelProvider;
    private readonly ILogger<RabbitMqConsumer<TService>> _logger;
    private readonly IReadingProcessingService _processingService;
    private readonly DataProcessorMetrics _metrics;

    public RabbitMqConsumer(
        string queueName,
        int maxRetries,
        IRabbitMqChannelProvider channelProvider,
        DataProcessorMetrics metrics,
        ILogger<RabbitMqConsumer<TService>> logger, IReadingProcessingService processingService)
    {
        _queueName = queueName;
        _maxRetries = maxRetries;
        _channelProvider = channelProvider;
        _metrics = metrics;
        _logger = logger;
        _processingService = processingService;
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

                await _processingService.ProcessReading(message, ct);

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
                await HandleFailureAsync(channel, ea, ex, ct);
            }
        };

        await channel.BasicConsumeAsync(
            queue: _queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: ct);

        _logger.LogInformation(
            "RabbitMQ consumer for {ProcessorType} is listening on queue {QueueName} (max retries {MaxRetries})",
            typeof(TService).Name,
            _queueName,
            _maxRetries);

        await Task.Delay(Timeout.Infinite, ct);
    }

    private async Task HandleFailureAsync(
        IChannel channel,
        BasicDeliverEventArgs ea,
        Exception ex,
        CancellationToken ct)
    {
        var attempt = MessageRetry.GetAttempt(ea.BasicProperties);

        if (attempt < _maxRetries)
        {
            var nextAttempt = attempt + 1;

            _logger.LogWarning(
                ex,
                "Processing failed on queue {QueueName} (delivery tag {DeliveryTag}); retry {Attempt}/{MaxRetries}",
                _queueName,
                ea.DeliveryTag,
                nextAttempt,
                _maxRetries);

            var retryProperties = MessageRetry.CreateRetryProperties(ea.BasicProperties, nextAttempt);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _queueName,
                mandatory: false,
                basicProperties: retryProperties,
                body: ea.Body,
                cancellationToken: ct);

            await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
            _metrics.RecordRetried(_queueName);
            return;
        }

        _logger.LogError(
            ex,
            "Processing failed on queue {QueueName} (delivery tag {DeliveryTag}) after {MaxRetries} retries; dead-lettering message",
            _queueName,
            ea.DeliveryTag,
            _maxRetries);

        await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, ct);
        _metrics.RecordDeadLettered(_queueName);
    }
}
