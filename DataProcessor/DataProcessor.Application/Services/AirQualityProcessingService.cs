using System.Text.Json;
using DataProcessor.Application.Dtos;
using DataProcessor.Application.Interfaces.Mappers;
using DataProcessor.Application.Interfaces.Repositories;
using DataProcessor.Application.Interfaces.Services;
using DataProcessor.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DataProcessor.Application.Services;

public class AirQualityProcessingService : IReadingProcessingService
{
    private readonly IAirQualityRepository _airQualityRepository;
    private readonly IMapper<AirQualityReadingDto, AirQualityReadingEntity> _mapper;
    private readonly ISensorNotificationService _notificationService;
    private readonly ILogger<AirQualityProcessingService> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public AirQualityProcessingService(
        IAirQualityRepository airQualityRepository,
        IMapper<AirQualityReadingDto, AirQualityReadingEntity> mapper,
        ISensorNotificationService notificationService,
        ILogger<AirQualityProcessingService> logger)
    {
        _airQualityRepository = airQualityRepository;
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
        AirQualityReadingDto? airQuality;
        try
        {
            airQuality = JsonSerializer.Deserialize<AirQualityReadingDto>(jsonData, _jsonSerializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize air quality reading payload");
            throw;
        }

        if (airQuality is null)
        {
            _logger.LogError("Received null air quality reading after deserialization");
            throw new InvalidOperationException("Cannot deserialize a null air quality reading!");
        }

        _logger.LogDebug(
            "Processing air quality reading for {Location} at {Timestamp}",
            airQuality.Name,
            airQuality.Timestamp);

        var entity = _mapper.Map(airQuality);
        await _airQualityRepository.AddAsync(entity, ct);

        await _notificationService.NotifyAirQualityProcessedAsync(airQuality, ct);

        _logger.LogInformation(
            "Stored air quality reading {ReadingId} for {Location}: CO2={Co2}, PM2.5={Pm25}, humidity={Humidity}% at {Timestamp}",
            entity.Id,
            airQuality.Name,
            airQuality.Payload.Co2,
            airQuality.Payload.Pm25,
            airQuality.Payload.Humidity,
            airQuality.Timestamp);
    }
}
