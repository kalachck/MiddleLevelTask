using System.Text.Json;
using DataProcessor.Application.Dtos;
using DataProcessor.Application.Interfaces;
using DataProcessor.Application.Interfaces.Mappers;
using DataProcessor.Application.Interfaces.Repositories;
using DataProcessor.Application.Interfaces.Services;
using DataProcessor.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DataProcessor.Application.Services;

public class EnergyProcessingService : IReadingProcessingService
{
    private readonly IEnergyRepository _energyRepository;
    private readonly IMapper<EnergyReadingDto, EnergyReadingEntity> _mapper;
    private readonly ISensorNotificationService _notificationService;
    private readonly ILogger<EnergyProcessingService> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public EnergyProcessingService(
        IEnergyRepository energyRepository,
        IMapper<EnergyReadingDto, EnergyReadingEntity> mapper,
        ISensorNotificationService notificationService,
        ILogger<EnergyProcessingService> logger)
    {
        _energyRepository = energyRepository;
        _mapper = mapper;
        _notificationService = notificationService;
        _logger = logger;

        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
    }

    public async Task ProcessReading(string jsonData, CancellationToken ct)
    {
        EnergyReadingDto? energy;
        try
        {
            energy = JsonSerializer.Deserialize<EnergyReadingDto>(jsonData, _jsonSerializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize energy reading payload");
            throw;
        }

        if (energy is null)
        {
            _logger.LogError("Received null energy reading after deserialization");
            throw new InvalidOperationException("Cannot deserialize a null energy reading!");
        }

        _logger.LogDebug(
            "Processing energy reading for {Location} at {Timestamp}",
            energy.Name,
            energy.Timestamp);

        var entity = _mapper.Map(energy);
        await _energyRepository.AddAsync(entity, ct);

        await _notificationService.NotifyEnergyProcessedAsync(energy, ct);

        _logger.LogInformation(
            "Stored energy reading {ReadingId} for {Location}: {Energy} kWh at {Timestamp}",
            entity.Id,
            energy.Name,
            energy.Payload.Energy,
            energy.Timestamp);
    }
}
