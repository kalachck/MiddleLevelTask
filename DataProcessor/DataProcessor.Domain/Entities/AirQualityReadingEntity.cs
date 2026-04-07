using DataProcessor.Domain.Entities.Abstract;

namespace DataProcessor.Domain.Entities;

public record AirQualityReadingEntity(
    string Name,
    int Co2,
    int Pm25,
    int Humidity,
    DateTime Timestamp) : BaseEntity;
