using Microsoft.Extensions.Options;

namespace NotificationService.Infrastructure.RabbitMq.Providers;

public interface IRabbitMqConfigProvider
{
    RabbitMqConfig GetRabbitMqConfig();
    NotificationsRabbitMqConfig GetNotificationsConfig();
}

public class RabbitMqConfigProvider : IRabbitMqConfigProvider
{
    private readonly RabbitMqConfig _rabbitMqConfig;
    private readonly NotificationsRabbitMqConfig _notificationsConfig;

    public RabbitMqConfigProvider(
        IOptions<RabbitMqConfig> rabbitMqConfig,
        IOptions<NotificationsRabbitMqConfig> notificationsConfig)
    {
        _rabbitMqConfig = rabbitMqConfig.Value;
        _notificationsConfig = notificationsConfig.Value;
    }

    public RabbitMqConfig GetRabbitMqConfig()
        => _rabbitMqConfig;

    public NotificationsRabbitMqConfig GetNotificationsConfig()
        => _notificationsConfig;
}
