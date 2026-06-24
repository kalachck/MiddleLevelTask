using Microsoft.Extensions.Configuration;

namespace DataProcessor.Infrastructure.ClickHouse;

public interface IClickHouseConfigProvider
{
    string GetConnectionString();
}

public class ClickHouseConfigProvider : IClickHouseConfigProvider
{
    private readonly string _connectionString;

    public ClickHouseConfigProvider(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ClickHouseConnection") ?? throw new ArgumentException("ClickHouse connection string is required!");
    }

    public string GetConnectionString()
        => _connectionString;
}
