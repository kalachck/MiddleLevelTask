using DataProcessor.Domain.Entities.Abstract;

namespace DataProcessor.Domain.Entities;

public record MotionReadingEntity(
    string Name,
    bool MotionDetected,
    DateTime Timestamp) : BaseEntity;
