using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace NotificationService.Infrastructure.RabbitMq.Providers;

public interface IRabbitMqChannelProvider
{
    Task<IChannel> GetChannel(CancellationToken ct);
}

public class RabbitMqChannelProvider : IRabbitMqChannelProvider
{
    private IChannel? _channel;
    private readonly RabbitMqConfig _config;
    private readonly ILogger<RabbitMqChannelProvider> _logger;

    public RabbitMqChannelProvider(
        IRabbitMqConfigProvider configProvider,
        ILogger<RabbitMqChannelProvider> logger)
    {
        _config = configProvider.GetRabbitMqConfig();
        _logger = logger;
    }

    public async Task<IChannel> GetChannel(CancellationToken ct)
    {
        if (_channel is not null)
        {
            return _channel;
        }

        var hostName = _config.HostName ?? "localhost";
        var port = _config.Port ?? 5672;

        _logger.LogInformation(
            "Opening RabbitMQ connection to {HostName}:{Port} as {UserName}",
            hostName,
            port,
            _config.UserName ?? "guest");

        var connectionFactory = new ConnectionFactory
        {
            HostName = hostName,
            Port = port,
            UserName = _config.UserName ?? "guest",
            Password = _config.Password ?? "guest",
        };

        var connection = await connectionFactory.CreateConnectionAsync(ct);
        _channel = await connection.CreateChannelAsync(cancellationToken: ct);

        _logger.LogInformation("RabbitMQ channel created successfully");

        return _channel;
    }
}
