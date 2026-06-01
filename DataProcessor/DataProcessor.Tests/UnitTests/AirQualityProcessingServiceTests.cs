using System.Text.Json;
using AutoFixture;
using DataProcessor.Application.Dtos;
using DataProcessor.Application.Interfaces.Mappers;
using DataProcessor.Application.Interfaces.Repositories;
using DataProcessor.Application.Interfaces.Services;
using DataProcessor.Application.Services;
using DataProcessor.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace DataProcessor.Tests.UnitTests;

public class AirQualityProcessingServiceTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly IAirQualityRepository _airQualityRepository = Substitute.For<IAirQualityRepository>();
    private readonly IMapper<AirQualityReadingDto, AirQualityReadingEntity> _mapper =
        Substitute.For<IMapper<AirQualityReadingDto, AirQualityReadingEntity>>();
    private readonly ISensorNotificationService _notificationService = Substitute.For<ISensorNotificationService>();
    private readonly ILogger<AirQualityProcessingService> _logger = NullLogger<AirQualityProcessingService>.Instance;

    private AirQualityProcessingService CreateSut() =>
        new(_airQualityRepository, _mapper, _notificationService, _logger);

    [Fact]
    public async Task ProcessReading_WhenJsonIsValid_ShouldMapDeserializedDtoToEntity()
    {
        // Arrange
        var dto = _fixture.Create<AirQualityReadingDto>();
        var entity = _fixture.Create<AirQualityReadingEntity>();
        var json = JsonSerializer.Serialize(dto);
        _mapper.Map(Arg.Any<AirQualityReadingDto>()).Returns(entity);
        var sut = CreateSut();

        // Act
        await sut.ProcessReading(json, CancellationToken.None);

        // Assert
        _mapper.Received(1).Map(Arg.Is<AirQualityReadingDto>(d =>
            d.Name == dto.Name &&
            d.Payload.Co2 == dto.Payload.Co2 &&
            d.Payload.Pm25 == dto.Payload.Pm25 &&
            d.Payload.Humidity == dto.Payload.Humidity &&
            d.Timestamp == dto.Timestamp));
    }

    [Fact]
    public async Task ProcessReading_WhenJsonIsValid_ShouldPersistMappedEntityToRepository()
    {
        // Arrange
        var dto = _fixture.Create<AirQualityReadingDto>();
        var entity = _fixture.Create<AirQualityReadingEntity>();
        var json = JsonSerializer.Serialize(dto);
        _mapper.Map(Arg.Any<AirQualityReadingDto>()).Returns(entity);
        using var cts = new CancellationTokenSource();
        var sut = CreateSut();

        // Act
        await sut.ProcessReading(json, cts.Token);

        // Assert
        await _airQualityRepository.Received(1).AddAsync(entity, cts.Token);
    }

    [Fact]
    public async Task ProcessReading_WhenJsonIsValid_ShouldNotifyAfterPersisting()
    {
        // Arrange
        var dto = _fixture.Create<AirQualityReadingDto>();
        var entity = _fixture.Create<AirQualityReadingEntity>();
        var json = JsonSerializer.Serialize(dto);
        _mapper.Map(Arg.Any<AirQualityReadingDto>()).Returns(entity);
        using var cts = new CancellationTokenSource();
        var sut = CreateSut();

        // Act
        await sut.ProcessReading(json, cts.Token);

        // Assert
        await _notificationService.Received(1).NotifyAirQualityProcessedAsync(
            Arg.Is<AirQualityReadingDto>(d =>
                d.Name == dto.Name &&
                d.Payload.Co2 == dto.Payload.Co2 &&
                d.Payload.Pm25 == dto.Payload.Pm25 &&
                d.Payload.Humidity == dto.Payload.Humidity &&
                d.Timestamp == dto.Timestamp),
            cts.Token);
    }

    [Fact]
    public async Task ProcessReading_WhenCalled_ShouldPersistBeforeNotifying()
    {
        // Arrange
        var dto = _fixture.Create<AirQualityReadingDto>();
        var entity = _fixture.Create<AirQualityReadingEntity>();
        var json = JsonSerializer.Serialize(dto);
        _mapper.Map(Arg.Any<AirQualityReadingDto>()).Returns(entity);

        var addCalled = false;
        var notifyCalledBeforeAdd = false;
        _airQualityRepository.AddAsync(Arg.Any<AirQualityReadingEntity>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                addCalled = true;
                return Task.CompletedTask;
            });
        _notificationService.NotifyAirQualityProcessedAsync(Arg.Any<AirQualityReadingDto>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (!addCalled) notifyCalledBeforeAdd = true;
                return Task.CompletedTask;
            });

        var sut = CreateSut();

        // Act
        await sut.ProcessReading(json, CancellationToken.None);

        // Assert
        Assert.True(addCalled);
        Assert.False(notifyCalledBeforeAdd);
    }

    [Fact]
    public async Task ProcessReading_WhenJsonIsMalformed_ShouldThrowJsonException()
    {
        // Arrange
        const string invalidJson = "{not-a-json}";
        var sut = CreateSut();

        // Act
        Func<Task> act = () => sut.ProcessReading(invalidJson, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<JsonException>(act);
        _mapper.DidNotReceive().Map(Arg.Any<AirQualityReadingDto>());
        await _airQualityRepository.DidNotReceive()
            .AddAsync(Arg.Any<AirQualityReadingEntity>(), Arg.Any<CancellationToken>());
        await _notificationService.DidNotReceive()
            .NotifyAirQualityProcessedAsync(Arg.Any<AirQualityReadingDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessReading_WhenJsonIsLiteralNull_ShouldThrowInvalidOperationException()
    {
        // Arrange
        const string nullJson = "null";
        var sut = CreateSut();

        // Act
        var act = () => sut.ProcessReading(nullJson, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
        _mapper.DidNotReceive().Map(Arg.Any<AirQualityReadingDto>());
        await _airQualityRepository.DidNotReceive()
            .AddAsync(Arg.Any<AirQualityReadingEntity>(), Arg.Any<CancellationToken>());
        await _notificationService.DidNotReceive()
            .NotifyAirQualityProcessedAsync(Arg.Any<AirQualityReadingDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessReading_WhenRepositoryThrows_ShouldNotInvokeNotification()
    {
        // Arrange
        var dto = _fixture.Create<AirQualityReadingDto>();
        var entity = _fixture.Create<AirQualityReadingEntity>();
        var json = JsonSerializer.Serialize(dto);
        _mapper.Map(Arg.Any<AirQualityReadingDto>()).Returns(entity);
        _airQualityRepository.AddAsync(Arg.Any<AirQualityReadingEntity>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("repo down"));
        var sut = CreateSut();

        // Act
        var act = () => sut.ProcessReading(json, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
        await _notificationService.DidNotReceive()
            .NotifyAirQualityProcessedAsync(Arg.Any<AirQualityReadingDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessReading_WhenJsonHasMixedCaseProperties_ShouldDeserializeCaseInsensitively()
    {
        // Arrange
        const string mixedCaseJson = """
            {
                "name": "Lobby",
                "payload": { "co2": 410, "pm25": 12, "humidity": 47 },
                "timestamp": "2026-01-01T12:00:00Z"
            }
            """;
        var entity = _fixture.Create<AirQualityReadingEntity>();
        _mapper.Map(Arg.Any<AirQualityReadingDto>()).Returns(entity);
        var sut = CreateSut();

        // Act
        await sut.ProcessReading(mixedCaseJson, CancellationToken.None);

        // Assert
        _mapper.Received(1).Map(Arg.Is<AirQualityReadingDto>(d =>
            d.Name == "Lobby" &&
            d.Payload.Co2 == 410 &&
            d.Payload.Pm25 == 12 &&
            d.Payload.Humidity == 47));
    }
}
