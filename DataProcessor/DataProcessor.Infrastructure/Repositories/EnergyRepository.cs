using Dapper;
using DataProcessor.Application.Interfaces;
using DataProcessor.Application.Interfaces.Repositories;
using DataProcessor.Domain.Entities;
using DataProcessor.Infrastructure.ClickHouse;

namespace DataProcessor.Infrastructure.Repositories;

public class EnergyRepository : IEnergyRepository
{
    private readonly IClickHouseConnectionFactory _connectionFactory;

    public EnergyRepository(IClickHouseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(EnergyReadingEntity entity, CancellationToken ct)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        const string sql = @"INSERT INTO EnergyReadings (Id, Name, Energy, CreatedAt, Timestamp) VALUES (@Id, @Name, @Energy, @CreatedAt, @Timestamp)";

        await connection.ExecuteAsync(sql, entity);
    }
}
