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

public class EnergyProcessingServiceTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly IEnergyRepository _energyRepository = Substitute.For<IEnergyRepository>();
    private readonly IMapper<EnergyReadingDto, EnergyReadingEntity> _mapper =
        Substitute.For<IMapper<EnergyReadingDto, EnergyReadingEntity>>();
    private readonly ISensorNotificationService _notificationService = Substitute.For<ISensorNotificationService>();
    private readonly ILogger<EnergyProcessingService> _logger = NullLogger<EnergyProcessingService>.Instance;

    private EnergyProcessingService CreateSut() =>
        new(_energyRepository, _mapper, _notificationService, _logger);

    [Fact]
    public async Task ProcessReading_WhenJsonIsValid_ShouldMapDeserializedDtoToEntity()
    {
        // Arrange
        var dto = _fixture.Create<EnergyReadingDto>();
        var entity = _fixture.Create<EnergyReadingEntity>();
        var json = JsonSerializer.Serialize(dto);
        _mapper.Map(Arg.Any<EnergyReadingDto>()).Returns(entity);
        var sut = CreateSut();

        // Act
        await sut.ProcessReading(json, CancellationToken.None);

        // Assert
        _mapper.Received(1).Map(Arg.Is<EnergyReadingDto>(d =>
            d.Name == dto.Name &&
            d.Payload.Energy == dto.Payload.Energy &&
            d.Timestamp == dto.Timestamp));
    }

    [Fact]
    public async Task ProcessReading_WhenJsonIsValid_ShouldPersistMappedEntityToRepository()
    {
        // Arrange
        var dto = _fixture.Create<EnergyReadingDto>();
        var entity = _fixture.Create<EnergyReadingEntity>();
        var json = JsonSerializer.Serialize(dto);
        _mapper.Map(Arg.Any<EnergyReadingDto>()).Returns(entity);
        using var cts = new CancellationTokenSource();
        var sut = CreateSut();

        // Act
        await sut.ProcessReading(json, cts.Token);

        // Assert
        await _energyRepository.Received(1).AddAsync(entity, cts.Token);
    }

    [Fact]
    public async Task ProcessReading_WhenJsonIsValid_ShouldNotifyAfterPersisting()
    {
        // Arrange
        var dto = _fixture.Create<EnergyReadingDto>();
        var entity = _fixture.Create<EnergyReadingEntity>();
        var json = JsonSerializer.Serialize(dto);
        _mapper.Map(Arg.Any<EnergyReadingDto>()).Returns(entity);
        using var cts = new CancellationTokenSource();
        var sut = CreateSut();

        // Act
        await sut.ProcessReading(json, cts.Token);

        // Assert
        await _notificationService.Received(1).NotifyEnergyProcessedAsync(
            Arg.Is<EnergyReadingDto>(d =>
                d.Name == dto.Name &&
                d.Payload.Energy == dto.Payload.Energy &&
                d.Timestamp == dto.Timestamp),
            cts.Token);
    }

    [Fact]
    public async Task ProcessReading_WhenCalled_ShouldPersistBeforeNotifying()
    {
        // Arrange
        var dto = _fixture.Create<EnergyReadingDto>();
        var entity = _fixture.Create<EnergyReadingEntity>();
        var json = JsonSerializer.Serialize(dto);
        _mapper.Map(Arg.Any<EnergyReadingDto>()).Returns(entity);

        var addCalled = false;
        var notifyCalledBeforeAdd = false;
        _energyRepository.AddAsync(Arg.Any<EnergyReadingEntity>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                addCalled = true;
                return Task.CompletedTask;
            });
        _notificationService.NotifyEnergyProcessedAsync(Arg.Any<EnergyReadingDto>(), Arg.Any<CancellationToken>())
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
        _mapper.DidNotReceive().Map(Arg.Any<EnergyReadingDto>());
        await _energyRepository.DidNotReceive().AddAsync(Arg.Any<EnergyReadingEntity>(), Arg.Any<CancellationToken>());
        await _notificationService.DidNotReceive()
            .NotifyEnergyProcessedAsync(Arg.Any<EnergyReadingDto>(), Arg.Any<CancellationToken>());
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
        _mapper.DidNotReceive().Map(Arg.Any<EnergyReadingDto>());
        await _energyRepository.DidNotReceive().AddAsync(Arg.Any<EnergyReadingEntity>(), Arg.Any<CancellationToken>());
        await _notificationService.DidNotReceive()
            .NotifyEnergyProcessedAsync(Arg.Any<EnergyReadingDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessReading_WhenRepositoryThrows_ShouldNotInvokeNotification()
    {
        // Arrange
        var dto = _fixture.Create<EnergyReadingDto>();
        var entity = _fixture.Create<EnergyReadingEntity>();
        var json = JsonSerializer.Serialize(dto);
        _mapper.Map(Arg.Any<EnergyReadingDto>()).Returns(entity);
        _energyRepository.AddAsync(Arg.Any<EnergyReadingEntity>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("repo down"));
        var sut = CreateSut();

        // Act
        var act = () => sut.ProcessReading(json, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
        await _notificationService.DidNotReceive()
            .NotifyEnergyProcessedAsync(Arg.Any<EnergyReadingDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessReading_WhenJsonHasMixedCaseProperties_ShouldDeserializeCaseInsensitively()
    {
        // Arrange
        const string mixedCaseJson = """
            {
                "name": "Office",
                "payload": { "energy": 12.50 },
                "timestamp": "2026-01-01T12:00:00Z"
            }
            """;
        var entity = _fixture.Create<EnergyReadingEntity>();
        _mapper.Map(Arg.Any<EnergyReadingDto>()).Returns(entity);
        var sut = CreateSut();

        // Act
        await sut.ProcessReading(mixedCaseJson, CancellationToken.None);

        // Assert
        _mapper.Received(1).Map(Arg.Is<EnergyReadingDto>(d =>
            d.Name == "Office" &&
            d.Payload.Energy == 12.50m));
    }
}
