using DataIngestor.Domain.Models;

namespace DataIngestor.Domain.Abstractions;

public interface IWeakApiClient
{
    Task<IEnumerable<SensorReading>> FetchReadingsAsync(CancellationToken ct = default);
}
