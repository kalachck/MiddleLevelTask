using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace DataProcessor.Application.Metrics;

public sealed class DataProcessorMetrics
{
    public const string MeterName = "DataProcessor";

    private readonly Counter<long> _messagesProcessed;
    private readonly Counter<long> _messagesFailed;
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
        _processingDuration = meter.CreateHistogram<double>(
            "processor.message.processing.duration",
            unit: "s",
            description: "Duration of message processing");
    }

    public void RecordProcessed(string queue)
        => _messagesProcessed.Add(1, new KeyValuePair<string, object?>("queue", queue));

    public void RecordFailed(string queue)
        => _messagesFailed.Add(1, new KeyValuePair<string, object?>("queue", queue));

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
