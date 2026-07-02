using DataProcessor.Application.Dtos;
using DataProcessor.Application.Interfaces.Mappers;
using DataProcessor.Domain.Entities;

namespace DataProcessor.Application.Mappers;

public class AirQualityReadingToEntityMapper : IMapper<AirQualityReadingDto, AirQualityReadingEntity>
{
    public AirQualityReadingEntity Map(AirQualityReadingDto input)
        => new(
            input.Name,
            input.Payload.Co2,
            input.Payload.Pm25,
            input.Payload.Humidity,
            input.Timestamp);
}

public class EnergyReadingToEntityMapper : IMapper<EnergyReadingDto, EnergyReadingEntity>
{
    public EnergyReadingEntity Map(EnergyReadingDto input)
        => new(input.Name, input.Payload.Energy, input.Timestamp);
}

public class MotionReadingToEntityMapper : IMapper<MotionReadingDto, MotionReadingEntity>
{
    public MotionReadingEntity Map(MotionReadingDto input)
        => new(input.Name, input.Payload.MotionDetected, input.Timestamp);
}
