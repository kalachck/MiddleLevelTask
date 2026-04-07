using DataProcessor.Domain.Entities;

namespace DataProcessor.Application.Interfaces.Repositories;

public interface IEnergyRepository
{
    Task AddAsync(EnergyReadingEntity entity, CancellationToken ct);
}
