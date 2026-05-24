using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DataIngestor.Domain.Abstractions;
using DataIngestor.Domain.Models;
using DataIngestor.Infrastructure.Configurations.Models;
using DataIngestor.Infrastructure.Configurations.Providers;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DataIngestor.Infrastructure;

public class RabbitMqPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly List<string> _sensorTypes = ["energy", "air_quality", "motion"];

    private readonly RabbitMqConfig _config;
    private readonly ConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqPublisher(
        IRabbitMqConfigProvider configProvider,
        ILogger<RabbitMqPublisher> logger)
    {
        _config = configProvider.GetRabbitMqConfig();
        _logger = logger;
        _connectionFactory = new ConnectionFactory
        {
            HostName = _config.HostName ?? "localhost",
            Port = _config.Port ?? 5672,
            UserName = _config.UserName ?? "guest",
            Password = _config.Password ?? "guest",
        };
    }

    public async Task PublishAsync(SensorReading sensorReading, CancellationToken ct)
    {
        if (_channel is null || !_channel.IsOpen)
        {
            await InitializeAsync(ct);
        }

        var json = JsonSerializer.Serialize(sensorReading);
        var body = Encoding.UTF8.GetBytes(json);

        var typeName = sensorReading.Type.ToString();
        var snakeCaseType = Regex.Replace(typeName, "(?<=.)([A-Z])", "_$1").ToLower();
        var routingKey = $"sensors.{snakeCaseType}";

        await _channel!.BasicPublishAsync(
            exchange: _config.ExchangeName,
            routingKey: routingKey,
            mandatory: true,
            body: body,
            cancellationToken: ct);

        _logger.LogDebug(
            "Published {SensorType} reading for {Location} to exchange {Exchange} with routing key {RoutingKey} ({ByteCount} bytes)",
            sensorReading.Type,
            sensorReading.Name,
            _config.ExchangeName,
            routingKey,
            body.Length);
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Closing RabbitMQ publisher connection");

        if (_channel is not null)
        {
            await _channel.CloseAsync();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
        }
    }

    private async Task InitializeAsync(CancellationToken ct)
    {
        var hostName = _config.HostName ?? "localhost";
        var port = _config.Port ?? 5672;

        _logger.LogInformation(
            "Connecting to RabbitMQ at {HostName}:{Port} as {UserName}",
            hostName,
            port,
            _config.UserName ?? "guest");

        _connection = await _connectionFactory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(cancellationToken: ct);

        await _channel.ExchangeDeclareAsync(
            exchange: _config.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: ct);

        _logger.LogDebug(
            "Declared topic exchange {ExchangeName}",
            _config.ExchangeName);

        foreach (var type in _sensorTypes)
        {
            var queueName = $"{_config.QueueName}.{type}";
            var routingKey = $"{_config.RoutingKeyPattern}.{type}";

            await _channel.QueueDeclareAsync(
                queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: ct);
            await _channel.QueueBindAsync(
                queueName,
                _config.ExchangeName,
                routingKey,
                cancellationToken: ct);

            _logger.LogDebug(
                "Bound queue {QueueName} to exchange {ExchangeName} with routing key {RoutingKey}",
                queueName,
                _config.ExchangeName,
                routingKey);
        }

        _logger.LogInformation(
            "RabbitMQ publisher ready (exchange {ExchangeName}, {QueueCount} queues)",
            _config.ExchangeName,
            _sensorTypes.Count);
    }
}
