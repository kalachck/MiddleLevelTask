using AutoFixture;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using NotificationService.Application.Dtos;
using NotificationService.Presentation.SignalR.Hubs;
using NotificationService.Presentation.SignalR.Services;
using NSubstitute;

namespace NotificationService.Tests.UnitTests;

public class SignalRHubBroadcasterTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly IHubContext<SensorsHub> _hubContext = Substitute.For<IHubContext<SensorsHub>>();
    private readonly IHubClients _hubClients = Substitute.For<IHubClients>();
    private readonly IClientProxy _allClientsProxy = Substitute.For<IClientProxy>();

    public SignalRHubBroadcasterTests()
    {
        _hubContext.Clients.Returns(_hubClients);
        _hubClients.All.Returns(_allClientsProxy);
    }

    private SignalRHubBroadcaster CreateSut() => new(_hubContext, NullLogger<SignalRHubBroadcaster>.Instance);

    [Fact]
    public async Task NotifyEnergyProcessedAsync_WhenInvoked_ShouldBroadcastToAllClientsWithExpectedMethod()
    {
        var dto = _fixture.Create<EnergyReadingDto>();
        using var cts = new CancellationTokenSource();
        var sut = CreateSut();

        await sut.NotifyEnergyProcessedAsync(dto, cts.Token);

        await _allClientsProxy.Received(1).SendCoreAsync(
            "NotifyEnergyProcessed",
            Arg.Any<object?[]>(),
            cts.Token);
    }

    [Fact]
    public async Task NotifyEnergyProcessedAsync_WhenInvoked_ShouldSendNamePayloadAndTimestampAsPayload()
    {
        var dto = new EnergyReadingDto
        {
            Name = "Office",
            Payload = new EnergyPayload { Energy = 12.5m },
            Timestamp = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
        };
        var sut = CreateSut();

        await sut.NotifyEnergyProcessedAsync(dto, CancellationToken.None);

        await _allClientsProxy.Received(1).SendCoreAsync(
            "NotifyEnergyProcessed",
            Arg.Is<object?[]>(args => args.Length == 1 && PayloadHasEnergyShape(args[0], dto)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyMotionProcessedAsync_WhenInvoked_ShouldBroadcastToAllClientsWithExpectedMethod()
    {
        var dto = _fixture.Create<MotionReadingDto>();
        using var cts = new CancellationTokenSource();
        var sut = CreateSut();

        await sut.NotifyMotionProcessedAsync(dto, cts.Token);

        await _allClientsProxy.Received(1).SendCoreAsync(
            "NotifyMotionProcessed",
            Arg.Any<object?[]>(),
            cts.Token);
    }

    [Fact]
    public async Task NotifyMotionProcessedAsync_WhenInvoked_ShouldSendNamePayloadAndTimestampAsPayload()
    {
        var dto = new MotionReadingDto
        {
            Name = "Hallway",
            Payload = new MotionPayload { MotionDetected = true },
            Timestamp = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
        };
        var sut = CreateSut();

        await sut.NotifyMotionProcessedAsync(dto, CancellationToken.None);

        await _allClientsProxy.Received(1).SendCoreAsync(
            "NotifyMotionProcessed",
            Arg.Is<object?[]>(args => args.Length == 1 && PayloadHasMotionShape(args[0], dto)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyAirQualityProcessedAsync_WhenInvoked_ShouldBroadcastToAllClientsWithExpectedMethod()
    {
        var dto = _fixture.Create<AirQualityReadingDto>();
        using var cts = new CancellationTokenSource();
        var sut = CreateSut();

        await sut.NotifyAirQualityProcessedAsync(dto, cts.Token);

        await _allClientsProxy.Received(1).SendCoreAsync(
            "NotifyAirQualityProcessed",
            Arg.Any<object?[]>(),
            cts.Token);
    }

    [Fact]
    public async Task NotifyAirQualityProcessedAsync_WhenInvoked_ShouldSendFlattenedPayloadFields()
    {
        var dto = new AirQualityReadingDto
        {
            Name = "Lobby",
            Payload = new AirQualityPayload { Co2 = 410, Pm25 = 12, Humidity = 47 },
            Timestamp = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
        };
        var sut = CreateSut();

        await sut.NotifyAirQualityProcessedAsync(dto, CancellationToken.None);

        await _allClientsProxy.Received(1).SendCoreAsync(
            "NotifyAirQualityProcessed",
            Arg.Is<object?[]>(args => args.Length == 1 && PayloadHasAirQualityShape(args[0], dto)),
            Arg.Any<CancellationToken>());
    }

    private static bool PayloadHasEnergyShape(object? payload, EnergyReadingDto expected)
    {
        if (payload is null) return false;
        var name = GetPropertyValue<string>(payload, "Name");
        var energy = GetPropertyValue<decimal>(payload, "Energy");
        var timestamp = GetPropertyValue<DateTime>(payload, "Timestamp");
        return name == expected.Name && energy == expected.Payload.Energy && timestamp == expected.Timestamp;
    }

    private static bool PayloadHasMotionShape(object? payload, MotionReadingDto expected)
    {
        if (payload is null) return false;
        var name = GetPropertyValue<string>(payload, "Name");
        var motion = GetPropertyValue<bool>(payload, "MotionDetected");
        var timestamp = GetPropertyValue<DateTime>(payload, "Timestamp");
        return name == expected.Name && motion == expected.Payload.MotionDetected && timestamp == expected.Timestamp;
    }

    private static bool PayloadHasAirQualityShape(object? payload, AirQualityReadingDto expected)
    {
        if (payload is null) return false;
        var name = GetPropertyValue<string>(payload, "name");
        var co2 = GetPropertyValue<int>(payload, "co2");
        var pm25 = GetPropertyValue<int>(payload, "pm25");
        var humidity = GetPropertyValue<int>(payload, "humidity");
        var timestamp = GetPropertyValue<DateTime>(payload, "timestamp");
        return name == expected.Name &&
               co2 == expected.Payload.Co2 &&
               pm25 == expected.Payload.Pm25 &&
               humidity == expected.Payload.Humidity &&
               timestamp == expected.Timestamp;
    }

    private static T? GetPropertyValue<T>(object source, string propertyName)
    {
        var prop = source.GetType().GetProperty(propertyName);
        if (prop is null) return default;
        var value = prop.GetValue(source);
        return value is T typed ? typed : default;
    }
}
