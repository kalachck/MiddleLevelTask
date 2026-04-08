using DataProcessor.Domain.Entities.Abstract;

namespace DataProcessor.Domain.Entities;

public record EnergyReadingEntity(
    string Name,
    decimal Energy,
    DateTime Timestamp) : BaseEntity;
