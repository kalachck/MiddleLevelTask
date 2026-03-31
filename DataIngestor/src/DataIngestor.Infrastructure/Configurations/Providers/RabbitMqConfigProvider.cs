using DataIngestor.Infrastructure.Configurations.Models;
using Microsoft.Extensions.Options;

namespace DataIngestor.Infrastructure.Configurations.Providers;

public interface IRabbitMqConfigProvider
{
    RabbitMqConfig GetRabbitMqConfig();
}

public class RabbitMqConfigProvider : IRabbitMqConfigProvider
{
    private readonly RabbitMqConfig _rabbitMqConfig;
    
    public RabbitMqConfigProvider(IOptions<RabbitMqConfig> options)
    {
        _rabbitMqConfig = options.Value;
    }
    
    public RabbitMqConfig GetRabbitMqConfig() 
        => _rabbitMqConfig;
}
