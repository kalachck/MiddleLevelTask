using System.Text;
using System.Text.Json;
using DataProcessor.Application.Dtos;
using DataProcessor.Application.Interfaces.Services;
using DataProcessor.Infrastructure.RabbitMq.Providers;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DataProcessor.Infrastructure.RabbitMq;

public class RabbitMqNotificationPublisher : ISensorNotificationService, IAsyncDisposable
{
    private readonly RabbitMqConfig _rabbitMqConfig;
    private readonly NotificationsRabbitMqConfig _notificationsConfig;
    private readonly ILogger<RabbitMqNotificationPublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private IConnection? _connection;
    private IChannel? _channel;
    private bool _initialized;

    public RabbitMqNotificationPublisher(
        IRabbitMqConfigProvider configProvider,
        ILogger<RabbitMqNotificationPublisher> logger)
    {
        _rabbitMqConfig = configProvider.GetRabbitMqConfig();
        _notificationsConfig = configProvider.GetNotificationsConfig();
        _logger = logger;
    }

    public async Task NotifyEnergyProcessedAsync(EnergyReadingDto energy, CancellationToken ct)
    {
        await PublishAsync("energy", energy, ct);
    }

    public async Task NotifyMotionProcessedAsync(MotionReadingDto motion, CancellationToken ct)
    {
        await PublishAsync("motion", motion, ct);
    }

    public async Task NotifyAirQualityProcessedAsync(AirQualityReadingDto airQuality, CancellationToken ct)
    {
        await PublishAsync("air_quality", airQuality, ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
        }
    }

    private async Task PublishAsync<T>(string notificationType, T payload, CancellationToken ct)
    {
        await EnsureInitializedAsync(ct);

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var body = Encoding.UTF8.GetBytes(json);
        var routingKey = $"{_notificationsConfig.RoutingKeyPattern}{notificationType}";

        await _channel!.BasicPublishAsync(
            exchange: _notificationsConfig.ExchangeName,
            routingKey: routingKey,
            mandatory: true,
            body: body,
            cancellationToken: ct);

        _logger.LogDebug(
            "Published {NotificationType} notification to exchange {Exchange} with routing key {RoutingKey}",
            notificationType,
            _notificationsConfig.ExchangeName,
            routingKey);
    }

    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_initialized)
        {
            return;
        }

        var connectionFactory = new ConnectionFactory
        {
            HostName = _rabbitMqConfig.HostName ?? "localhost",
            Port = _rabbitMqConfig.Port ?? 5672,
            UserName = _rabbitMqConfig.UserName ?? "guest",
            Password = _rabbitMqConfig.Password ?? "guest",
        };

        _connection = await connectionFactory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(cancellationToken: ct);

        await _channel.ExchangeDeclareAsync(
            exchange: _notificationsConfig.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: ct);

        _initialized = true;

        _logger.LogInformation(
            "RabbitMQ notification publisher ready (exchange {ExchangeName})",
            _notificationsConfig.ExchangeName);
    }
}
