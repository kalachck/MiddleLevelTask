namespace DataProcessor.Infrastructure.RabbitMq;

public class NotificationsRabbitMqConfig
{
    public const string SectionName = "Notifications";

    public string ExchangeName { get; set; } = "notifications_exchange";
    public string RoutingKeyPattern { get; set; } = "notifications.";
}
