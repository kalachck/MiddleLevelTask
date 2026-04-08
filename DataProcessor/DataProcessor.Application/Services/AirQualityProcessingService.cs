using System.Text.Json;
using DataProcessor.Application.Dtos;
using DataProcessor.Application.Interfaces.Mappers;
using DataProcessor.Application.Interfaces.Repositories;
using DataProcessor.Application.Interfaces.Services;
using DataProcessor.Domain.Entities;

namespace DataProcessor.Application.Services;

public class AirQualityProcessingService : IReadingProcessingService
{
    private readonly IAirQualityRepository _airQualityRepository;
    private readonly IMapper<AirQualityReadingDto, AirQualityReadingEntity> _mapper;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public AirQualityProcessingService(
        IAirQualityRepository airQualityRepository,
        IMapper<AirQualityReadingDto, AirQualityReadingEntity> mapper)
    {
        _airQualityRepository = airQualityRepository;
        _mapper = mapper;

        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }
    
    public async Task ProcessReading(string jsonData, CancellationToken ct)
    {
        var airQuality = JsonSerializer.Deserialize<AirQualityReadingDto>(jsonData, _jsonSerializerOptions) 
                         ??  throw new InvalidOperationException("Cannot deserialize a null air quality reading!");
        
        await _airQualityRepository.AddAsync(_mapper.Map(airQuality), ct);
    }
}
