using NotificationService.Application.Metrics;
using Microsoft.AspNetCore.SignalR;

namespace NotificationService.Presentation.SignalR.Hubs;

public class SensorsHub : Hub
{
    private readonly ILogger<SensorsHub> _logger;
    private readonly NotificationMetrics _metrics;

    public SensorsHub(ILogger<SensorsHub> logger, NotificationMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    public override Task OnConnectedAsync()
    {
        _metrics.RecordConnection("connected");
        _logger.LogDebug(
            "SignalR client connected: {ConnectionId}",
            Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _metrics.RecordConnection("disconnected");

        if (exception is not null)
        {
            _logger.LogWarning(
                exception,
                "SignalR client disconnected with error: {ConnectionId}",
                Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation(
                "SignalR client disconnected: {ConnectionId}",
                Context.ConnectionId);
        }

        return base.OnDisconnectedAsync(exception);
    }
}
