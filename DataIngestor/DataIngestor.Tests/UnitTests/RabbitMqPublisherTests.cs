using System.Reflection;
using System.Text;
using System.Text.Json;
using AutoFixture;
using DataIngestor.Domain.Models;
using DataIngestor.Infrastructure;
using DataIngestor.Infrastructure.Configurations.Models;
using DataIngestor.Infrastructure.Configurations.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RabbitMQ.Client;

namespace DataIngestor.Tests.UnitTests;

public class RabbitMqPublisherTests
{
    private const string ExchangeName = "sensors-exchange";

    private readonly IFixture _fixture = new Fixture();
    private readonly IRabbitMqConfigProvider _configProvider = Substitute.For<IRabbitMqConfigProvider>();
    private readonly RabbitMqConfig _config;

    public RabbitMqPublisherTests()
    {
        _config = new RabbitMqConfig
        {
            ExchangeName = ExchangeName,
            QueueName = "sensors-queue",
            RoutingKeyPattern = "sensors",
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest",
        };
        _configProvider.GetRabbitMqConfig().Returns(_config);
    }

    [Theory]
    [InlineData(SensorType.Energy, "sensors.energy")]
    [InlineData(SensorType.Motion, "sensors.motion")]
    [InlineData(SensorType.AirQuality, "sensors.air_quality")]
    [InlineData(SensorType.Unknown, "sensors.unknown")]
    public async Task PublishAsync_WhenChannelIsOpen_ShouldPublishWithExpectedRoutingKey(
        SensorType sensorType,
        string expectedRoutingKey)
    {
        // Arrange
        var channel = CreateOpenChannelMock();
        var publisher = new RabbitMqPublisher(_configProvider, NullLogger<RabbitMqPublisher>.Instance);
        SetChannel(publisher, channel);

        var reading = new SensorReading(sensorType, _fixture.Create<string>(), new { value = 42 });

        // Act
        await publisher.PublishAsync(reading, CancellationToken.None);

        // Assert
        var publishCall = GetPublishCall(channel);
        var args = publishCall.GetArguments();
        Assert.Equal(ExchangeName, args[0]);
        Assert.Equal(expectedRoutingKey, args[1]);
        Assert.Equal(true, args[2]);
    }

    [Fact]
    public async Task PublishAsync_WhenInvoked_ShouldSerializeReadingAsJsonBody()
    {
        // Arrange
        var channel = CreateOpenChannelMock();
        var publisher = new RabbitMqPublisher(_configProvider, NullLogger<RabbitMqPublisher>.Instance);
        SetChannel(publisher, channel);

        var reading = new SensorReading(SensorType.Energy, "Office", new { value = 7 });

        // Act
        await publisher.PublishAsync(reading, CancellationToken.None);

        // Assert
        var publishCall = GetPublishCall(channel);
        var body = ExtractBody(publishCall.GetArguments());
        var json = Encoding.UTF8.GetString(body.Span);
        var parsed = JsonDocument.Parse(json).RootElement;
        Assert.Equal("Office", parsed.GetProperty("Name").GetString());
    }

    [Fact]
    public async Task PublishAsync_WhenCalledMultipleTimes_ShouldNotReinitializeAlreadyOpenChannel()
    {
        // Arrange
        var channel = CreateOpenChannelMock();
        var publisher = new RabbitMqPublisher(_configProvider, NullLogger<RabbitMqPublisher>.Instance);
        SetChannel(publisher, channel);

        var reading = _fixture.Build<SensorReading>()
            .FromFactory(() => new SensorReading(SensorType.Energy, "loc", new { v = 1 }))
            .Create();

        // Act
        await publisher.PublishAsync(reading, CancellationToken.None);
        await publisher.PublishAsync(reading, CancellationToken.None);

        // Assert
        var publishCalls = channel
            .ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IChannel.BasicPublishAsync))
            .ToList();
        Assert.Equal(2, publishCalls.Count);

        var exchangeDeclareCalls = channel
            .ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IChannel.ExchangeDeclareAsync))
            .ToList();
        Assert.Empty(exchangeDeclareCalls);
    }

    [Fact]
    public async Task PublishAsync_WhenCancellationTokenProvided_ShouldForwardItToChannel()
    {
        // Arrange
        var channel = CreateOpenChannelMock();
        var publisher = new RabbitMqPublisher(_configProvider, NullLogger<RabbitMqPublisher>.Instance);
        SetChannel(publisher, channel);

        var reading = new SensorReading(SensorType.Energy, "loc", new { value = 1 });
        using var cts = new CancellationTokenSource();

        // Act
        await publisher.PublishAsync(reading, cts.Token);

        // Assert
        var publishCall = GetPublishCall(channel);
        var args = publishCall.GetArguments();
        var forwardedToken = args.OfType<CancellationToken>().First();
        Assert.Equal(cts.Token, forwardedToken);
    }

    [Fact]
    public async Task DisposeAsync_WhenChannelAndConnectionAreNull_ShouldNotThrow()
    {
        // Arrange
        var publisher = new RabbitMqPublisher(_configProvider, NullLogger<RabbitMqPublisher>.Instance);

        // Act
        var act = async () => await publisher.DisposeAsync();

        // Assert
        await act();
    }

    [Fact]
    public async Task DisposeAsync_WhenChannelAndConnectionExist_ShouldCloseBoth()
    {
        // Arrange
        var channel = CreateOpenChannelMock();
        var connection = Substitute.For<IConnection>();
        var publisher = new RabbitMqPublisher(_configProvider, NullLogger<RabbitMqPublisher>.Instance);
        SetChannel(publisher, channel);
        SetConnection(publisher, connection);

        // Act
        await publisher.DisposeAsync();

        // Assert
        await channel.Received(1).CloseAsync(Arg.Any<CancellationToken>());
        await connection.Received(1).CloseAsync(Arg.Any<CancellationToken>());
    }

    private static IChannel CreateOpenChannelMock()
    {
        var channel = Substitute.For<IChannel>();
        channel.IsOpen.Returns(true);
        return channel;
    }

    private static void SetChannel(RabbitMqPublisher publisher, IChannel channel)
    {
        var field = typeof(RabbitMqPublisher).GetField("_channel", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("_channel field not found");
        field.SetValue(publisher, channel);
    }

    private static void SetConnection(RabbitMqPublisher publisher, IConnection connection)
    {
        var field = typeof(RabbitMqPublisher).GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("_connection field not found");
        field.SetValue(publisher, connection);
    }

    private static NSubstitute.Core.ICall GetPublishCall(IChannel channel)
    {
        return channel
            .ReceivedCalls()
            .Single(c => c.GetMethodInfo().Name == nameof(IChannel.BasicPublishAsync));
    }

    private static ReadOnlyMemory<byte> ExtractBody(object?[] args)
    {
        foreach (var arg in args)
        {
            if (arg is ReadOnlyMemory<byte> rom)
            {
                return rom;
            }
        }
        throw new InvalidOperationException("No ReadOnlyMemory<byte> body argument found in BasicPublishAsync call");
    }
}
