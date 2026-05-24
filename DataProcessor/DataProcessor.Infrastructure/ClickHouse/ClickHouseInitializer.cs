using Dapper;
using Microsoft.Extensions.Logging;

namespace DataProcessor.Infrastructure.ClickHouse;

public interface IClickHouseInitializer
{
    Task InitializeAsync();
}

public sealed class ClickHouseInitializer : IClickHouseInitializer
{
    private readonly IClickHouseConnectionFactory _factory;
    private readonly ILogger<ClickHouseInitializer> _logger;

    public ClickHouseInitializer(
        IClickHouseConnectionFactory factory,
        ILogger<ClickHouseInitializer> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Connecting to ClickHouse to ensure schema exists");

        await using var connection = _factory.CreateConnection();
        await connection.OpenAsync();

        const string createDbSql = "CREATE DATABASE IF NOT EXISTS SensorsReadings";
        await connection.ExecuteAsync(createDbSql);
        _logger.LogDebug("Ensured database SensorsReadings exists");

        const string createEnergyTableSql =
            @"CREATE TABLE IF NOT EXISTS SensorsReadings.EnergyReadings (Id UUID, Name String, Energy Decimal(12, 2), CreatedAt DateTime, Timestamp DateTime) ENGINE MergeTree() ORDER BY Id";
        const string createMotionTableSql =
            @"CREATE TABLE IF NOT EXISTS SensorsReadings.MotionReadings (Id UUID, Name String, MotionDetected Bool, CreatedAt DateTime, Timestamp DateTime) ENGINE MergeTree() ORDER BY Id";
        const string createAirQualityTableSql =
            @"CREATE TABLE IF NOT EXISTS SensorsReadings.AirQualityReadings (Id UUID, Name String, Co2 Int, Pm25 Int, Humidity Int, CreatedAt DateTime, Timestamp DateTime) ENGINE MergeTree() ORDER BY Id";

        await connection.ExecuteAsync(createEnergyTableSql);
        await connection.ExecuteAsync(createMotionTableSql);
        await connection.ExecuteAsync(createAirQualityTableSql);

        _logger.LogInformation(
            "ClickHouse tables ready: EnergyReadings, MotionReadings, AirQualityReadings");
    }
}
