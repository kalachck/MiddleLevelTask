namespace NotificationService.Infrastructure.RabbitMq;

public class RabbitMqConfig
{
    public const string SectionName = "RabbitMq";
    public const string AttemptHeader = "x-retry-attempt";

    public string? HostName { get; set; }
    public int? Port { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
}

public class NotificationsRabbitMqConfig
{
    public const string SectionName = "Notifications";

    public string QueueName { get; set; } = "notifications_queue";
    public string ExchangeName { get; set; } = "notifications_exchange";
    public string RoutingKeyPattern { get; set; } = "notifications.";
    public string? DeadLetterExchange { get; set; }
    public int MaxRetries { get; set; } = 3;
}
