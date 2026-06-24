using Dapper;
using DataProcessor.Application.Interfaces;
using DataProcessor.Application.Interfaces.Repositories;
using DataProcessor.Domain.Entities;
using DataProcessor.Infrastructure.ClickHouse;

namespace DataProcessor.Infrastructure.Repositories;

public class MotionRepository : IMotionRepository
{
    private readonly IClickHouseConnectionFactory _connectionFactory;

    public MotionRepository(IClickHouseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(MotionReadingEntity entity, CancellationToken ct)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        const string sql = @"INSERT INTO MotionReadings (Id, Name, MotionDetected, CreatedAt, Timestamp) VALUES (@Id, @Name, @MotionDetected, @CreatedAt, @Timestamp)";

        await connection.ExecuteAsync(sql, entity);
    }
}
