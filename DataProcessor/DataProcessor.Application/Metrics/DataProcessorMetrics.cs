using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace DataProcessor.Application.Metrics;

public sealed class DataProcessorMetrics
{
    public const string MeterName = "DataProcessor";

    private readonly Counter<long> _messagesProcessed;
    private readonly Counter<long> _messagesFailed;
    private readonly Counter<long> _messagesRetried;
    private readonly Counter<long> _messagesDeadLettered;
    private readonly Histogram<double> _processingDuration;

    public DataProcessorMetrics()
    {
        var meter = new Meter(MeterName);

        _messagesProcessed = meter.CreateCounter<long>(
            "processor.messages.processed",
            description: "Total number of messages successfully processed");
        _messagesFailed = meter.CreateCounter<long>(
            "processor.messages.failed",
            description: "Total number of messages that failed processing");
        _messagesRetried = meter.CreateCounter<long>(
            "processor.messages.retried",
            description: "Total number of messages scheduled for retry");
        _messagesDeadLettered = meter.CreateCounter<long>(
            "processor.messages.deadlettered",
            description: "Total number of messages routed to the dead-letter queue");
        _processingDuration = meter.CreateHistogram<double>(
            "processor.message.processing.duration",
            unit: "s",
            description: "Duration of message processing");
    }

    public void RecordProcessed(string queue)
        => _messagesProcessed.Add(1, new KeyValuePair<string, object?>("queue", queue));

    public void RecordFailed(string queue)
        => _messagesFailed.Add(1, new KeyValuePair<string, object?>("queue", queue));

    public void RecordRetried(string queue)
        => _messagesRetried.Add(1, new KeyValuePair<string, object?>("queue", queue));

    public void RecordDeadLettered(string queue)
        => _messagesDeadLettered.Add(1, new KeyValuePair<string, object?>("queue", queue));

    public IDisposable TrackProcessingDuration(string queue)
        => new DurationScope(duration => _processingDuration.Record(duration, new KeyValuePair<string, object?>("queue", queue)));

    private sealed class DurationScope(Action<double> record) : IDisposable
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public void Dispose()
        {
            _stopwatch.Stop();
            record(_stopwatch.Elapsed.TotalSeconds);
        }
    }
}
