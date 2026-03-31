using DataIngestor.Domain.Models;

namespace DataIngestor.Domain.Abstractions;

public interface IMessagePublisher
{
    Task PublishAsync(SensorReading sensorReading, CancellationToken ct);
}
