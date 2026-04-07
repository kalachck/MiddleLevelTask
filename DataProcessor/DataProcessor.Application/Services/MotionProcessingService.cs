using System.Text.Json;
using DataProcessor.Application.Dtos;
using DataProcessor.Application.Interfaces;
using DataProcessor.Application.Interfaces.Mappers;
using DataProcessor.Application.Interfaces.Repositories;
using DataProcessor.Application.Interfaces.Services;
using DataProcessor.Domain.Entities;

namespace DataProcessor.Application.Services;

public class MotionProcessingService : IReadingProcessingService
{
    private readonly IMotionRepository _motionRepository;
    private readonly IMapper<MotionReadingDto, MotionReadingEntity> _mapper;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public MotionProcessingService(
        IMotionRepository motionRepository,
        IMapper<MotionReadingDto, MotionReadingEntity> mapper)
    {
        _motionRepository = motionRepository;
        _mapper = mapper;

        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
    }
    
    public async Task ProcessReading(string jsonData, CancellationToken ct)
    {
        var motion = JsonSerializer.Deserialize<MotionReadingDto>(jsonData, _jsonSerializerOptions) ?? throw new InvalidOperationException("Cannot deserialize a null motion reading!");
        
        await _motionRepository.AddAsync(_mapper.Map(motion), ct);
    }
}
