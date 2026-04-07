using RabbitMQ.Client;

namespace DataProcessor.Infrastructure.RabbitMq.Providers;

public interface IRabbitMqChannelProvider
{
    Task<IChannel> GetChannel(CancellationToken ct);
}

public class RabbitMqChannelProvider : IRabbitMqChannelProvider
{
    private IChannel _channel;
    private readonly RabbitMqConfig _config;
    
    public RabbitMqChannelProvider(IRabbitMqConfigProvider configProvider)
    {
        _config = configProvider.GetRabbitMqConfig();
    }
    
    public async Task<IChannel> GetChannel(CancellationToken ct)
    {
        if (_channel == null)
        {
            _channel = await CreateChannel(ct);
        }

        return _channel;
    }
    
    private async Task<IChannel> CreateChannel(CancellationToken ct)
    {
        var connectionFactory = new ConnectionFactory()
        {
            HostName = _config.HostName ?? "localhost",
            Port = _config.Port ?? 5672,
            UserName = _config.UserName ?? "guest",
            Password = _config.Password ?? "guest"
        };
        
        var connection = await connectionFactory.CreateConnectionAsync(ct);

        return await connection.CreateChannelAsync(cancellationToken: ct);
    }
}
