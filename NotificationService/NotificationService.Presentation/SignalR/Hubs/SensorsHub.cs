using Microsoft.AspNetCore.SignalR;

namespace NotificationService.Presentation.SignalR.Hubs;

public class SensorsHub : Hub
{
    private readonly ILogger<SensorsHub> _logger;

    public SensorsHub(ILogger<SensorsHub> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "SignalR client connected: {ConnectionId}",
            Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
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
