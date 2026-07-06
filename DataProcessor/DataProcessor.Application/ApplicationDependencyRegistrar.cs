using DataProcessor.Application.Dtos;
using DataProcessor.Application.Interfaces.Mappers;
using DataProcessor.Application.Interfaces.Services;
using DataProcessor.Application.Mappers;
using DataProcessor.Application.Metrics;
using DataProcessor.Application.Services;
using DataProcessor.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace DataProcessor.Application;

public static class ApplicationDependencyRegistrar
{
    public static void AddApplicationDependencies(this IServiceCollection services)
    {
        services.AddSingleton<DataProcessorMetrics>();

        services.AddScoped<IReadingProcessingService, AirQualityProcessingService>();
        services.AddScoped<IReadingProcessingService, MotionProcessingService>();
        services.AddScoped<IReadingProcessingService, EnergyProcessingService>();

        services.AddScoped<IMapper<AirQualityReadingDto, AirQualityReadingEntity>, AirQualityReadingToEntityMapper>();
        services.AddScoped<IMapper<MotionReadingDto, MotionReadingEntity>, MotionReadingToEntityMapper>();
        services.AddScoped<IMapper<EnergyReadingDto, EnergyReadingEntity>, EnergyReadingToEntityMapper>();
    }
}
