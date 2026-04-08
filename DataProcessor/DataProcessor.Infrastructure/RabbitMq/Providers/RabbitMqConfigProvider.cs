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
}

public class RabbitMqConfigProvider : IRabbitMqConfigProvider
{
    private readonly RabbitMqConfig _config;
    
    public RabbitMqConfigProvider(IOptions<RabbitMqConfig> config)
    {
        _config = config.Value;
    }
    
    public RabbitMqConfig GetRabbitMqConfig()
    {
        return _config;
    }
}
