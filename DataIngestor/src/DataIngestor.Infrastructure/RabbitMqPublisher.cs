using System.Text;
using System.Text.Json;
using DataIngestor.Domain.Abstractions;
using DataIngestor.Domain.Models;
using DataIngestor.Infrastructure.Configurations.Models;
using DataIngestor.Infrastructure.Configurations.Providers;
using RabbitMQ.Client;

namespace DataIngestor.Infrastructure;

public class RabbitMqPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly RabbitMqConfig _config;
    private readonly ConnectionFactory _connectionFactory;
    private IConnection? _connection;
    private IChannel? _channel;
    
    public RabbitMqPublisher(IRabbitMqConfigProvider configProvider)
    {
        _config = configProvider.GetRabbitMqConfig();
        _connectionFactory = new ConnectionFactory()
        {
            HostName = _config.HostName ?? "localhost",
            Port = _config.Port ?? 5672,
            UserName = _config.UserName ?? "guest",
            Password = _config.Password ?? "guest"
        };
    }
    
    
    public async Task PublishAsync(SensorReading sensorReading, CancellationToken ct)
    {
        if (_channel is null || !_channel.IsOpen)
        {
            await InitializeAsync();
        }
        
        var json = JsonSerializer.Serialize(sensorReading);
        var body = Encoding.UTF8.GetBytes(json);
        
        var routingKey = $"sensors.{sensorReading.Type.ToString().ToLower()}";

        await _channel!.BasicPublishAsync(
            exchange: _config.ExchangeName,
            routingKey: routingKey,
            mandatory: true,
            body: body,
            cancellationToken: ct);
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.CloseAsync();
        if (_connection is not null) await _connection.CloseAsync();
    }
    
    private async Task InitializeAsync()
    {
        _connection = await _connectionFactory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();
        
        await _channel.ExchangeDeclareAsync(
            exchange: _config.ExchangeName,
            type: ExchangeType.Topic,
            durable: true);

        await _channel.QueueDeclareAsync(
            queue: _config.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        
        await _channel.QueueBindAsync(
            queue: _config.QueueName,
            exchange: _config.ExchangeName,
            routingKey: _config.RoutingKeyPattern);
    }
}
