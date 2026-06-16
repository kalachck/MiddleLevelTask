using NotificationService.Application.Dtos;

namespace NotificationService.Application.Interfaces;

public interface ISensorHubBroadcaster
{
    Task NotifyEnergyProcessedAsync(EnergyReadingDto energy, CancellationToken ct);
    Task NotifyMotionProcessedAsync(MotionReadingDto motion, CancellationToken ct);
    Task NotifyAirQualityProcessedAsync(AirQualityReadingDto airQuality, CancellationToken ct);
}
