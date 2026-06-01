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

public class MotionProcessingServiceTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly IMotionRepository _motionRepository = Substitute.For<IMotionRepository>();
    private readonly IMapper<MotionReadingDto, MotionReadingEntity> _mapper =
        Substitute.For<IMapper<MotionReadingDto, MotionReadingEntity>>();
    private readonly ISensorNotificationService _notificationService = Substitute.For<ISensorNotificationService>();
    private readonly ILogger<MotionProcessingService> _logger = NullLogger<MotionProcessingService>.Instance;

    private MotionProcessingService CreateSut() =>
        new(_motionRepository, _mapper, _notificationService, _logger);

    [Fact]
    public async Task ProcessReading_WhenJsonIsValid_ShouldMapDeserializedDtoToEntity()
    {
        // Arrange
        var dto = _fixture.Create<MotionReadingDto>();
        var entity = _fixture.Create<MotionReadingEntity>();
        var json = JsonSerializer.Serialize(dto);
        _mapper.Map(Arg.Any<MotionReadingDto>()).Returns(entity);
        var sut = CreateSut();

        // Act
        await sut.ProcessReading(json, CancellationToken.None);

        // Assert
        _mapper.Received(1).Map(Arg.Is<MotionReadingDto>(d =>
            d.Name == dto.Name &&
            d.Payload.MotionDetected == dto.Payload.MotionDetected &&
            d.Timestamp == dto.Timestamp));
    }

    [Fact]
    public async Task ProcessReading_WhenJsonIsValid_ShouldPersistMappedEntityToRepository()
    {
        // Arrange
        var dto = _fixture.Create<MotionReadingDto>();
        var entity = _fixture.Create<MotionReadingEntity>();
        var json = JsonSerializer.Serialize(dto);
        _mapper.Map(Arg.Any<MotionReadingDto>()).Returns(entity);
        using var cts = new CancellationTokenSource();
        var sut = CreateSut();

        // Act
        await sut.ProcessReading(json, cts.Token);

        // Assert
        await _motionRepository.Received(1).AddAsync(entity, cts.Token);
    }

    [Fact]
    public async Task ProcessReading_WhenJsonIsValid_ShouldNotifyAfterPersisting()
    {
        // Arrange
        var dto = _fixture.Create<MotionReadingDto>();
        var entity = _fixture.Create<MotionReadingEntity>();
        var json = JsonSerializer.Serialize(dto);
        _mapper.Map(Arg.Any<MotionReadingDto>()).Returns(entity);
        using var cts = new CancellationTokenSource();
        var sut = CreateSut();

        // Act
        await sut.ProcessReading(json, cts.Token);

        // Assert
        await _notificationService.Received(1).NotifyMotionProcessedAsync(
            Arg.Is<MotionReadingDto>(d =>
                d.Name == dto.Name &&
                d.Payload.MotionDetected == dto.Payload.MotionDetected &&
                d.Timestamp == dto.Timestamp),
            cts.Token);
    }

    [Fact]
    public async Task ProcessReading_WhenCalled_ShouldPersistBeforeNotifying()
    {
        // Arrange
        var dto = _fixture.Create<MotionReadingDto>();
        var entity = _fixture.Create<MotionReadingEntity>();
        var json = JsonSerializer.Serialize(dto);
        _mapper.Map(Arg.Any<MotionReadingDto>()).Returns(entity);

        var addCalled = false;
        var notifyCalledBeforeAdd = false;
        _motionRepository.AddAsync(Arg.Any<MotionReadingEntity>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                addCalled = true;
                return Task.CompletedTask;
            });
        _notificationService.NotifyMotionProcessedAsync(Arg.Any<MotionReadingDto>(), Arg.Any<CancellationToken>())
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
        var act = () => sut.ProcessReading(invalidJson, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<JsonException>(act);
        _mapper.DidNotReceive().Map(Arg.Any<MotionReadingDto>());
        await _motionRepository.DidNotReceive().AddAsync(Arg.Any<MotionReadingEntity>(), Arg.Any<CancellationToken>());
        await _notificationService.DidNotReceive()
            .NotifyMotionProcessedAsync(Arg.Any<MotionReadingDto>(), Arg.Any<CancellationToken>());
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
        _mapper.DidNotReceive().Map(Arg.Any<MotionReadingDto>());
        await _motionRepository.DidNotReceive().AddAsync(Arg.Any<MotionReadingEntity>(), Arg.Any<CancellationToken>());
        await _notificationService.DidNotReceive()
            .NotifyMotionProcessedAsync(Arg.Any<MotionReadingDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessReading_WhenRepositoryThrows_ShouldNotInvokeNotification()
    {
        // Arrange
        var dto = _fixture.Create<MotionReadingDto>();
        var entity = _fixture.Create<MotionReadingEntity>();
        var json = JsonSerializer.Serialize(dto);
        _mapper.Map(Arg.Any<MotionReadingDto>()).Returns(entity);
        _motionRepository.AddAsync(Arg.Any<MotionReadingEntity>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("repo down"));
        var sut = CreateSut();

        // Act
        var act = () => sut.ProcessReading(json, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
        await _notificationService.DidNotReceive()
            .NotifyMotionProcessedAsync(Arg.Any<MotionReadingDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessReading_WhenJsonHasMixedCaseProperties_ShouldDeserializeCaseInsensitively()
    {
        // Arrange
        const string mixedCaseJson = """
            {
                "name": "Hallway",
                "payload": { "motionDetected": true },
                "timestamp": "2026-01-01T12:00:00Z"
            }
            """;
        var entity = _fixture.Create<MotionReadingEntity>();
        _mapper.Map(Arg.Any<MotionReadingDto>()).Returns(entity);
        var sut = CreateSut();

        // Act
        await sut.ProcessReading(mixedCaseJson, CancellationToken.None);

        // Assert
        _mapper.Received(1).Map(Arg.Is<MotionReadingDto>(d =>
            d.Name == "Hallway" &&
            d.Payload.MotionDetected));
    }
}
