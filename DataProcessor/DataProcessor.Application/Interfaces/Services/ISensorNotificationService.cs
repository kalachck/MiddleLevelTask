using DataProcessor.Application.Dtos;

namespace DataProcessor.Application.Interfaces.Services;

public interface ISensorNotificationService
{
    Task NotifyEnergyProcessedAsync(EnergyReadingDto energy, CancellationToken ct);
    Task NotifyMotionProcessedAsync(MotionReadingDto motion, CancellationToken ct);
    Task NotifyAirQualityProcessedAsync(AirQualityReadingDto airQuality, CancellationToken ct);
}
