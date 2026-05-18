using DataProcessor.Application.Dtos;
using DataProcessor.Application.Interfaces.Services;
using DataProcessor.Presentation.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace DataProcessor.Presentation.SignalR.Services;

public class SignalRNotificationService : ISensorNotificationService
{
    private readonly IHubContext<SensorsHub> _hubContext;

    public SignalRNotificationService(IHubContext<SensorsHub> hubContext)
    {
        _hubContext = hubContext;
    }
    
    public async Task NotifyEnergyProcessedAsync(EnergyReadingDto energy, CancellationToken ct)
    {
        await _hubContext.Clients.All.SendAsync("NotifyEnergyProcessed", new
        {
            energy.Name,
            energy.Payload.Energy,
            energy.Timestamp
        }, ct);
    }

    public async Task NotifyMotionProcessedAsync(MotionReadingDto motion, CancellationToken ct)
    {
        await _hubContext.Clients.All.SendAsync("NotifyMotionProcessed", new
        {
            motion.Name,
            motion.Payload.MotionDetected,
            motion.Timestamp
        },  ct);
    }

    public async Task NotifyAirQualityProcessedAsync(AirQualityReadingDto airQuality, CancellationToken ct)
    {
        await _hubContext.Clients.All.SendAsync("NotifyAirQualityProcessed", new
        {
            airQuality.Name,
            airQuality.Payload.Co2,
            airQuality.Payload.Pm25,
            airQuality.Payload.Humidity,
            airQuality.Timestamp
        }, ct);
    }
}
