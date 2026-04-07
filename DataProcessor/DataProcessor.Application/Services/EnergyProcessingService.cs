using System.Text.Json;
using DataProcessor.Application.Dtos;
using DataProcessor.Application.Interfaces;
using DataProcessor.Application.Interfaces.Mappers;
using DataProcessor.Application.Interfaces.Repositories;
using DataProcessor.Application.Interfaces.Services;
using DataProcessor.Domain.Entities;

namespace DataProcessor.Application.Services;

public class EnergyProcessingService : IReadingProcessingService
{
    private readonly IEnergyRepository _energyRepository;
    private readonly IMapper<EnergyReadingDto, EnergyReadingEntity> _mapper;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public EnergyProcessingService(
        IEnergyRepository energyRepository,
        IMapper<EnergyReadingDto, EnergyReadingEntity> mapper)
    {
        _energyRepository = energyRepository;
        _mapper = mapper;

        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive =  true,
        };
    }
    
    public async Task ProcessReading(string jsonData, CancellationToken ct)
    {
        var energy = JsonSerializer.Deserialize<EnergyReadingDto>(jsonData, _jsonSerializerOptions) 
                     ?? throw new InvalidOperationException("Cannot deserialize a null energy reading!");
        
        await _energyRepository.AddAsync(_mapper.Map(energy), ct);
    }
}
