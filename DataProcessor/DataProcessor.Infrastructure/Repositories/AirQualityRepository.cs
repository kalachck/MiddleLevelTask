using Dapper;
using DataProcessor.Application.Interfaces;
using DataProcessor.Application.Interfaces.Repositories;
using DataProcessor.Domain.Entities;
using DataProcessor.Infrastructure.ClickHouse;

namespace DataProcessor.Infrastructure.Repositories;

public class AirQualityRepository : IAirQualityRepository
{
    private readonly IClickHouseConnectionFactory _connectionFactory;

    public AirQualityRepository(IClickHouseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(AirQualityReadingEntity entity, CancellationToken ct)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        const string sql = @"INSERT INTO AirQualityReadings (Id, Name, Co2, Pm25, Humidity, CreatedAt, Timestamp) VALUES (@Id, @Name, @Co2, @Pm25, @Humidity, @CreatedAt, @Timestamp)";

        await connection.ExecuteAsync(sql, entity);
    }
}
