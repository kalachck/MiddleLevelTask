using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace DataIngestor.Application.Metrics;

public sealed class IngestionMetrics
{
    public const string MeterName = "DataIngestor";

    private readonly Counter<long> _readingsFetched;
    private readonly Counter<long> _readingsPublished;
    private readonly Counter<long> _pollCycles;
    private readonly Counter<long> _pollErrors;
    private readonly Histogram<double> _pollDuration;

    public IngestionMetrics()
    {
        var meter = new Meter(MeterName);

        _readingsFetched = meter.CreateCounter<long>(
            name: "ingestor.readings.fetched",
            description: "Total number of readings fetched from the weak API");
        _readingsPublished = meter.CreateCounter<long>(
            name: "ingestor.readings.published",
            description: "Total number of readings published to RabbitMQ");
        _pollCycles = meter.CreateCounter<long>(
            name: "ingestor.poll.cycles",
            description: "Total number of ingestion poll cycles completed");
        _pollErrors = meter.CreateCounter<long>(
            name: "ingestor.poll.errors",
            description: "Total number of failed ingestion poll cycles");
        _pollDuration = meter.CreateHistogram<double>(
            name: "ingestor.poll.duration",
            unit: "s",
            description: "Duration of ingestion poll cycles");
    }

    public void RecordReadingsFetched(int count, string sensorType)
        => _readingsFetched.Add(count, new KeyValuePair<string, object?>("sensor_type", sensorType));

    public void RecordReadingPublished(string sensorType)
        => _readingsPublished.Add(1, new KeyValuePair<string, object?>("sensor_type", sensorType));

    public void RecordPollCycle()
        => _pollCycles.Add(1);

    public void RecordPollError()
        => _pollErrors.Add(1);

    public IDisposable TrackPollDuration()
        => new DurationScope(duration => _pollDuration.Record(duration));

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
