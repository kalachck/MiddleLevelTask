using AutoFixture;
using DataIngestor.Application;
using DataIngestor.Application.Configurations.Models;
using DataIngestor.Application.Metrics;
using DataIngestor.Domain.Abstractions;
using DataIngestor.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DataIngestor.Tests.UnitTests;

public class IngestionWorkerTests
{
    private readonly IFixture _fixture;
    private readonly IWeakApiClient _weakApiClient = Substitute.For<IWeakApiClient>();
    private readonly IMessagePublisher _messagePublisher = Substitute.For<IMessagePublisher>();
    private readonly ILogger<IngestionWorker> _logger = NullLogger<IngestionWorker>.Instance;

    public IngestionWorkerTests()
    {
        _fixture = new Fixture();
        _fixture.Customize<SensorReading>(c => c.FromFactory(() =>
            new SensorReading(SensorType.Energy, _fixture.Create<string>(), new { value = _fixture.Create<int>() })));
    }

    [Fact]
    public async Task ExecuteAsync_WhenIntervalIsZero_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut(TimeSpan.Zero);

        // Act
        var act = () => sut.RunExecuteAsync(CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task ExecuteAsync_WhenIntervalIsNegative_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut(TimeSpan.FromMilliseconds(-1));

        // Act
        var act = () => sut.RunExecuteAsync(CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task ExecuteAsync_WhenReadingsAvailable_ShouldPublishEachReadingExactlyOnce()
    {
        // Arrange
        var readings = _fixture.CreateMany<SensorReading>(3).ToList();
        _weakApiClient.FetchReadingsAsync(Arg.Any<CancellationToken>()).Returns(readings);

        var publishedCount = 0;
        var allPublishedTcs = new TaskCompletionSource();
        _messagePublisher
            .PublishAsync(Arg.Any<SensorReading>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (Interlocked.Increment(ref publishedCount) >= readings.Count)
                {
                    allPublishedTcs.TrySetResult();
                }
                return Task.CompletedTask;
            });

        var sut = CreateSut(TimeSpan.FromSeconds(30));
        using var cts = new CancellationTokenSource();

        // Act
        var executeTask = sut.RunExecuteAsync(cts.Token);
        await allPublishedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await cts.CancelAsync();
        await SafeAwait(executeTask);

        // Assert
        foreach (var reading in readings)
        {
            await _messagePublisher
                .Received(1)
                .PublishAsync(reading, Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task ExecuteAsync_WhenReadingsAvailable_ShouldPassStoppingTokenToFetchAndPublish()
    {
        // Arrange
        var reading = _fixture.Create<SensorReading>();
        _weakApiClient.FetchReadingsAsync(Arg.Any<CancellationToken>())
            .Returns([reading]);

        var publishedTcs = new TaskCompletionSource();
        _messagePublisher
            .PublishAsync(Arg.Any<SensorReading>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                publishedTcs.TrySetResult();
                return Task.CompletedTask;
            });

        var sut = CreateSut(TimeSpan.FromSeconds(30));
        using var cts = new CancellationTokenSource();

        // Act
        var executeTask = sut.RunExecuteAsync(cts.Token);
        await publishedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5), cts.Token);
        await cts.CancelAsync();
        await SafeAwait(executeTask);

        // Assert
        await _weakApiClient.Received().FetchReadingsAsync(cts.Token);
        await _messagePublisher.Received(1).PublishAsync(reading, cts.Token);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoReadings_ShouldNotInvokePublisher()
    {
        // Arrange
        var fetchedTcs = new TaskCompletionSource();
        _weakApiClient.FetchReadingsAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                fetchedTcs.TrySetResult();
                return Task.FromResult<IEnumerable<SensorReading>>(Enumerable.Empty<SensorReading>());
            });

        var sut = CreateSut(TimeSpan.FromSeconds(30));
        using var cts = new CancellationTokenSource();

        // Act
        var executeTask = sut.RunExecuteAsync(cts.Token);
        await fetchedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5), cts.Token);
        await cts.CancelAsync();
        await SafeAwait(executeTask);

        // Assert
        await _messagePublisher
            .DidNotReceive()
            .PublishAsync(Arg.Any<SensorReading>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenFetchThrows_ShouldSwallowAndContinueOnNextTick()
    {
        // Arrange
        var callCount = 0;
        var secondCallTcs = new TaskCompletionSource();
        _weakApiClient.FetchReadingsAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var current = Interlocked.Increment(ref callCount);
                if (current == 1)
                {
                    throw new InvalidOperationException("simulated failure");
                }
                secondCallTcs.TrySetResult();
                return Task.FromResult<IEnumerable<SensorReading>>(Enumerable.Empty<SensorReading>());
            });

        var sut = CreateSut(TimeSpan.FromMilliseconds(50));
        using var cts = new CancellationTokenSource();

        // Act
        var executeTask = sut.RunExecuteAsync(cts.Token);
        await secondCallTcs.Task.WaitAsync(TimeSpan.FromSeconds(5), cts.Token);
        await cts.CancelAsync();
        await SafeAwait(executeTask);

        // Assert
        Assert.True(callCount >= 2, "Worker should keep running after a fetch failure");
        await _messagePublisher
            .DidNotReceive()
            .PublishAsync(Arg.Any<SensorReading>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenPublishThrows_ShouldSwallowAndContinueOnNextTick()
    {
        // Arrange
        var reading = _fixture.Create<SensorReading>();
        _weakApiClient.FetchReadingsAsync(Arg.Any<CancellationToken>())
            .Returns([reading]);

        var publishCount = 0;
        var secondPublishTcs = new TaskCompletionSource();
        _messagePublisher
            .PublishAsync(Arg.Any<SensorReading>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var current = Interlocked.Increment(ref publishCount);
                if (current == 1)
                {
                    throw new InvalidOperationException("publish failure");
                }
                secondPublishTcs.TrySetResult();
                return Task.CompletedTask;
            });

        var sut = CreateSut(TimeSpan.FromMilliseconds(50));
        using var cts = new CancellationTokenSource();

        // Act
        var executeTask = sut.RunExecuteAsync(cts.Token);
        await secondPublishTcs.Task.WaitAsync(TimeSpan.FromSeconds(5), cts.Token);
        await cts.CancelAsync();
        await SafeAwait(executeTask);

        // Assert
        Assert.True(publishCount >= 2, "Worker should keep running after a publish failure");
    }

    [Fact]
    public async Task ExecuteAsync_WhenStoppingTokenCancelledBeforeFirstTick_ShouldFinishWithoutPublishing()
    {
        // Arrange
        _weakApiClient.FetchReadingsAsync(Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<SensorReading>());

        var sut = CreateSut(TimeSpan.FromSeconds(30));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var executeTask = sut.RunExecuteAsync(cts.Token);
        await SafeAwait(executeTask);

        // Assert
        await _messagePublisher
            .DidNotReceive()
            .PublishAsync(Arg.Any<SensorReading>(), Arg.Any<CancellationToken>());
    }

    private readonly IngestionMetrics _metrics = new();

    private TestableIngestionWorker CreateSut(TimeSpan interval)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_weakApiClient);
        services.AddSingleton(_messagePublisher);
        var serviceProvider = services.BuildServiceProvider();

        var options = Options.Create(new IngestionOptions { Interval = interval });
        return new TestableIngestionWorker(serviceProvider, _logger, _metrics, options);
    }

    private static async Task SafeAwait(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // Expected when the stopping token is canceled while the periodic timer awaits the next tick.
        }
    }

    private sealed class TestableIngestionWorker : IngestionWorker
    {
        public TestableIngestionWorker(
            IServiceProvider serviceProvider,
            ILogger<IngestionWorker> logger,
            IngestionMetrics metrics,
            IOptions<IngestionOptions> options)
            : base(serviceProvider, logger, metrics, options)
        {
        }

        public Task RunExecuteAsync(CancellationToken stoppingToken) => ExecuteAsync(stoppingToken);
    }
}
