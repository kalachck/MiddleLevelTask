using System.Text;
using System.Text.Json;
using NotificationService.Application.Dtos;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Metrics;
using NotificationService.Infrastructure.RabbitMq.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService.Infrastructure.RabbitMq;

public class NotificationConsumer<TDto> : BackgroundService
{
    private readonly string _queueName;
    private readonly string _notificationType;
    private readonly int _maxRetries;
    private readonly IRabbitMqChannelProvider _channelProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationConsumer<TDto>> _logger;
    private readonly NotificationMetrics _metrics;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public NotificationConsumer(
        string queueName,
        string notificationType,
        int maxRetries,
        IRabbitMqChannelProvider channelProvider,
        IServiceProvider serviceProvider,
        NotificationMetrics metrics,
        ILogger<NotificationConsumer<TDto>> logger)
    {
        _queueName = queueName;
        _notificationType = notificationType;
        _maxRetries = maxRetries;
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
            try
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());

                _logger.LogDebug(
                    "Received {NotificationType} notification on queue {QueueName} (delivery tag {DeliveryTag})",
                    _notificationType,
                    _queueName,
                    ea.DeliveryTag);

                using var scope = _serviceProvider.CreateScope();
                var broadcaster = scope.ServiceProvider.GetRequiredService<ISensorHubBroadcaster>();

                await DispatchAsync(broadcaster, message, ct);

                await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                _metrics.RecordDispatched(_notificationType);
            }
            catch (Exception ex)
            {
                _metrics.RecordFailed(_notificationType);
                await HandleFailureAsync(channel, ea, ex, ct);
            }
        };

        await channel.BasicConsumeAsync(
            queue: _queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: ct);

        _logger.LogInformation(
            "Notification consumer for {NotificationType} is listening on queue {QueueName} (max retries {MaxRetries})",
            _notificationType,
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
                "Failed to process {NotificationType} notification on queue {QueueName}; retry {Attempt}/{MaxRetries}",
                _notificationType,
                _queueName,
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
            _metrics.RecordRetried(_notificationType);
            return;
        }

        _logger.LogError(
            ex,
            "Failed to process {NotificationType} notification on queue {QueueName} after {MaxRetries} retries; dead-lettering message",
            _notificationType,
            _queueName,
            _maxRetries);

        await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, ct);
        _metrics.RecordDeadLettered(_notificationType);
    }

    private Task DispatchAsync(ISensorHubBroadcaster broadcaster, string message, CancellationToken ct)
    {
        return _notificationType switch
        {
            "energy" => DispatchEnergyAsync(broadcaster, message, ct),
            "motion" => DispatchMotionAsync(broadcaster, message, ct),
            "air_quality" => DispatchAirQualityAsync(broadcaster, message, ct),
            _ => throw new InvalidOperationException($"Unknown notification type: {_notificationType}"),
        };
    }

    private async Task DispatchEnergyAsync(ISensorHubBroadcaster broadcaster, string message, CancellationToken ct)
    {
        var dto = JsonSerializer.Deserialize<EnergyReadingDto>(message, _jsonOptions)
            ?? throw new InvalidOperationException("Cannot deserialize energy notification payload.");
        await broadcaster.NotifyEnergyProcessedAsync(dto, ct);
    }

    private async Task DispatchMotionAsync(ISensorHubBroadcaster broadcaster, string message, CancellationToken ct)
    {
        var dto = JsonSerializer.Deserialize<MotionReadingDto>(message, _jsonOptions)
            ?? throw new InvalidOperationException("Cannot deserialize motion notification payload.");
        await broadcaster.NotifyMotionProcessedAsync(dto, ct);
    }

    private async Task DispatchAirQualityAsync(ISensorHubBroadcaster broadcaster, string message, CancellationToken ct)
    {
        var dto = JsonSerializer.Deserialize<AirQualityReadingDto>(message, _jsonOptions)
            ?? throw new InvalidOperationException("Cannot deserialize air quality notification payload.");
        await broadcaster.NotifyAirQualityProcessedAsync(dto, ct);
    }
}
