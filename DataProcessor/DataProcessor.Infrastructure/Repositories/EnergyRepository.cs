using System.Threading.Channels;
using Dapper;
using DataProcessor.Application.Interfaces.Repositories;
using DataProcessor.Domain.Entities;
using DataProcessor.Infrastructure.ClickHouse;
using Microsoft.Extensions.Hosting;

namespace DataProcessor.Infrastructure.Repositories;

public class EnergyRepository : IEnergyRepository, IHostedService
{
    private readonly IClickHouseConnectionFactory _connectionFactory;
    private readonly Channel<EnergyReadingEntity> _channel;
    private readonly int _batchSize = 100;
    private readonly TimeSpan _batchTimeout = TimeSpan.FromSeconds(5);
    private Task? _backgroundTask;
    private CancellationTokenSource? _cts;

    public EnergyRepository(IClickHouseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
        _channel = Channel.CreateBounded<EnergyReadingEntity>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public async Task AddAsync(EnergyReadingEntity entity, CancellationToken ct)
    {
        await _channel.Writer.WriteAsync(entity, ct);
    }

    public Task StartAsync(CancellationToken ct)
    {
        _cts = new CancellationTokenSource();
        _backgroundTask = StartBatchingLoopAsync(_cts.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _channel.Writer.Complete();

        try
        {
            if (_backgroundTask != null)
            {
                await Task.WhenAny(_backgroundTask, Task.Delay(Timeout.Infinite, ct));
            }
        }
        finally
        {
            _cts?.Cancel(false);
            _cts?.Dispose();
        }
    }

    private async Task StartBatchingLoopAsync(CancellationToken ct)
    {
        var buffer = new List<EnergyReadingEntity>(_batchSize);

        while (await _channel.Reader.WaitToReadAsync(ct))
        {
            var timeoutTask = Task.Delay(_batchTimeout, ct);

            while (buffer.Count < _batchSize && !timeoutTask.IsCompleted)
            {
                if (_channel.Reader.TryRead(out var item))
                    buffer.Add(item);
                else
                    await Task.Delay(10, ct);
            }

            if (buffer.Count > 0)
            {
                await using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync(ct);
                const string sql = @"INSERT INTO EnergyReadings (Id, Name, Energy, CreatedAt, Timestamp) VALUES (@Id, @Name, @Energy, @CreatedAt, @Timestamp)";

                await connection.ExecuteAsync(sql, buffer);
                buffer.Clear();
            }
        }
    }
}
