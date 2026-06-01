using DataProcessor.Application.Dtos;
using DataProcessor.Application.Interfaces.Services;
using DataProcessor.Presentation.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace DataProcessor.Presentation.SignalR.Services;

public class SignalRNotificationService : ISensorNotificationService
{
    private readonly IHubContext<SensorsHub> _hubContext;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(
        IHubContext<SensorsHub> hubContext,
        ILogger<SignalRNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyEnergyProcessedAsync(EnergyReadingDto energy, CancellationToken ct)
    {
        _logger.LogDebug(
            "Broadcasting energy update for {Location} to SignalR clients",
            energy.Name);

        await _hubContext.Clients.All.SendAsync("NotifyEnergyProcessed", new
        {
            energy.Name,
            energy.Payload.Energy,
            energy.Timestamp,
        }, ct);
    }

    public async Task NotifyMotionProcessedAsync(MotionReadingDto motion, CancellationToken ct)
    {
        _logger.LogDebug(
            "Broadcasting motion update for {Location} to SignalR clients",
            motion.Name);

        await _hubContext.Clients.All.SendAsync("NotifyMotionProcessed", new
        {
            motion.Name,
            motion.Payload.MotionDetected,
            motion.Timestamp,
        }, ct);
    }

    public async Task NotifyAirQualityProcessedAsync(AirQualityReadingDto airQuality, CancellationToken ct)
    {
        _logger.LogDebug(
            "Broadcasting air quality update for {Location} to SignalR clients",
            airQuality.Name);

        await _hubContext.Clients.All.SendAsync("NotifyAirQualityProcessed", new
        {
            name = airQuality.Name,
            co2 = airQuality.Payload.Co2,
            pm25 = airQuality.Payload.Pm25,
            humidity = airQuality.Payload.Humidity,
            timestamp = airQuality.Timestamp,
        }, ct);
    }
}
