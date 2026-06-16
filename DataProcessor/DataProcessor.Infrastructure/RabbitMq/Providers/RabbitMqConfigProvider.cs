using DataProcessor.Infrastructure.RabbitMq;
using Microsoft.Extensions.Options;

namespace DataProcessor.Infrastructure.RabbitMq.Providers;

public class RabbitMqConfig
{
    public const string SectionName = "RabbitMq";

    public string QueueName { get; set; }
    public string ExchangeName { get; set; }
    public string RoutingKeyPattern { get; set; }
    public string? HostName { get; set; }
    public int? Port { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
}

public interface IRabbitMqConfigProvider
{
    RabbitMqConfig GetRabbitMqConfig();
    NotificationsRabbitMqConfig GetNotificationsConfig();
}

public class RabbitMqConfigProvider : IRabbitMqConfigProvider
{
    private readonly RabbitMqConfig _config;
    private readonly NotificationsRabbitMqConfig _notificationsConfig;

    public RabbitMqConfigProvider(
        IOptions<RabbitMqConfig> config,
        IOptions<NotificationsRabbitMqConfig> notificationsConfig)
    {
        _config = config.Value;
        _notificationsConfig = notificationsConfig.Value;
    }

    public RabbitMqConfig GetRabbitMqConfig() => _config;

    public NotificationsRabbitMqConfig GetNotificationsConfig() => _notificationsConfig;
}
