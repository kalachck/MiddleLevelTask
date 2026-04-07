using Dapper;

namespace DataProcessor.Infrastructure.ClickHouse;

public interface IClickHouseInitializer
{
    Task InitializeAsync();
}

public sealed class ClickHouseInitializer : IClickHouseInitializer
{
    private readonly IClickHouseConnectionFactory _factory;
    
    public ClickHouseInitializer(IClickHouseConnectionFactory factory)
    {
        _factory = factory;
    }
    
    public async Task InitializeAsync()
    {
        await using var connection = _factory.CreateConnection();
        await connection.OpenAsync();

        const string createDbSql = "CREATE DATABASE IF NOT EXISTS SensorsReadings";
        await connection.ExecuteAsync(createDbSql);
        
        const string createEnergyTableSql = 
            @"CREATE TABLE IF NOT EXISTS SensorsReadings.EnergyReadings (Id UUID, Name String, Energy Decimal(12, 2), CreatedAt DateTime, Timestamp DateTime) ENGINE MergeTree() ORDER BY Id";
        const string createMotionTableSql =
            @"CREATE TABLE IF NOT EXISTS SensorsReadings.MotionReadings (Id UUID, Name String, MotionDetected Bool, CreatedAt DateTime, Timestamp DateTime) ENGINE MergeTree() ORDER BY Id";
        const string createAirQualityTableSql = 
            @"CREATE TABLE IF NOT EXISTS SensorsReadings.AirQualityReadings (Id UUID, Name String, Co2 Int, Pm25 Int, Humidity Int, CreatedAt DateTime, Timestamp DateTime) ENGINE MergeTree() ORDER BY Id";
        
        
        await connection.ExecuteAsync(createEnergyTableSql);
        await connection.ExecuteAsync(createMotionTableSql);
        await connection.ExecuteAsync(createAirQualityTableSql);
    }
}
