using DataProcessor.Domain.Entities;

namespace DataProcessor.Application.Interfaces.Repositories;

public interface IAirQualityRepository
{
    Task AddAsync(AirQualityReadingEntity entity, CancellationToken ct);
}
