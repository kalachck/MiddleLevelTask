using System.Diagnostics.Metrics;

namespace NotificationService.Application.Metrics;

public sealed class NotificationMetrics
{
    public const string MeterName = "NotificationService";

    private readonly Counter<long> _notificationsDispatched;
    private readonly Counter<long> _notificationsFailed;
    private readonly Counter<long> _signalrConnections;

    public NotificationMetrics()
    {
        var meter = new Meter(MeterName);
        _notificationsDispatched = meter.CreateCounter<long>(
            "notifications.dispatched",
            description: "Total number of notifications dispatched via SignalR");
        _notificationsFailed = meter.CreateCounter<long>(
            "notifications.failed",
            description: "Total number of notifications that failed to dispatch");
        _signalrConnections = meter.CreateCounter<long>(
            "signalr.connections",
            description: "Total number of SignalR hub connections");
    }

    public void RecordDispatched(string notificationType) =>
        _notificationsDispatched.Add(1, new KeyValuePair<string, object?>("type", notificationType));

    public void RecordFailed(string notificationType) =>
        _notificationsFailed.Add(1, new KeyValuePair<string, object?>("type", notificationType));

    public void RecordConnection(string eventType) =>
        _signalrConnections.Add(1, new KeyValuePair<string, object?>("event", eventType));
}
