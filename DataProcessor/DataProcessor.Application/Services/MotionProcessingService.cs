using System.Text.Json;
using DataProcessor.Application.Dtos;
using DataProcessor.Application.Interfaces;
using DataProcessor.Application.Interfaces.Mappers;
using DataProcessor.Application.Interfaces.Repositories;
using DataProcessor.Application.Interfaces.Services;
using DataProcessor.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DataProcessor.Application.Services;

public class MotionProcessingService : IReadingProcessingService
{
    private readonly IMotionRepository _motionRepository;
    private readonly IMapper<MotionReadingDto, MotionReadingEntity> _mapper;
    private readonly ISensorNotificationService _notificationService;
    private readonly ILogger<MotionProcessingService> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public MotionProcessingService(
        IMotionRepository motionRepository,
        IMapper<MotionReadingDto, MotionReadingEntity> mapper,
        ISensorNotificationService notificationService,
        ILogger<MotionProcessingService> logger)
    {
        _motionRepository = motionRepository;
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
        MotionReadingDto? motion;
        try
        {
            motion = JsonSerializer.Deserialize<MotionReadingDto>(jsonData, _jsonSerializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize motion reading payload");
            throw;
        }

        if (motion is null)
        {
            _logger.LogError("Received null motion reading after deserialization");
            throw new InvalidOperationException("Cannot deserialize a null motion reading!");
        }

        _logger.LogDebug(
            "Processing motion reading for {Location} at {Timestamp}",
            motion.Name,
            motion.Timestamp);

        var entity = _mapper.Map(motion);
        await _motionRepository.AddAsync(entity, ct);

        await _notificationService.NotifyMotionProcessedAsync(motion, ct);

        _logger.LogInformation(
            "Stored motion reading {ReadingId} for {Location}: motion={MotionDetected} at {Timestamp}",
            entity.Id,
            motion.Name,
            motion.Payload.MotionDetected,
            motion.Timestamp);
    }
}
