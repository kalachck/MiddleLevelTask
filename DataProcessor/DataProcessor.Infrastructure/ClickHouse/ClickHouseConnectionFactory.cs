using ClickHouse.Client.ADO;

namespace DataProcessor.Infrastructure.ClickHouse;

public interface IClickHouseConnectionFactory
{
    public ClickHouseConnection CreateConnection();
}

public class ClickHouseConnectionFactory : IClickHouseConnectionFactory
{
    private readonly IClickHouseConfigProvider _configProvider;

    public ClickHouseConnectionFactory(IClickHouseConfigProvider configProvider)
    {
        _configProvider = configProvider;
    }

    public ClickHouseConnection CreateConnection()
        => new(_configProvider.GetConnectionString());
}
