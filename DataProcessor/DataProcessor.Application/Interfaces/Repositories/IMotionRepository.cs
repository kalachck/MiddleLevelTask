using DataProcessor.Domain.Entities;

namespace DataProcessor.Application.Interfaces.Repositories;

public interface IMotionRepository
{
    Task AddAsync(MotionReadingEntity entity, CancellationToken ct);
}
