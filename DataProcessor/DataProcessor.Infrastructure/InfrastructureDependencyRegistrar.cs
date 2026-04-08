using DataProcessor.Application.Interfaces.Repositories;
using DataProcessor.Application.Services;
using DataProcessor.Infrastructure.ClickHouse;
using DataProcessor.Infrastructure.RabbitMq;
using DataProcessor.Infrastructure.RabbitMq.Providers;
using DataProcessor.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DataProcessor.Infrastructure;

public static class InfrastructureDependencyRegistrar
{
    public static void AddInfrastructureDependencies(this IServiceCollection services)
    {
        services
            .AddRabbitMq()
            .AddDatabaseDependencies();
    }

    public static void AddInfrastructureAppDependencies(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var databaseInitializer = scope.ServiceProvider.GetRequiredService<IClickHouseInitializer>();
        databaseInitializer.InitializeAsync();
    }

    private static IServiceCollection AddRabbitMq(this IServiceCollection services)
    {
        // services.AddHostedService(sp => new RabbitMqConsumer<MotionProcessingService>(
        //     queueName: "sensors_queue.motion",
        //     channelProvider: sp.GetRequiredService<IRabbitMqChannelProvider>(),
        //     sp));

        // services.AddHostedService(sp => new RabbitMqConsumer<EnergyProcessingService>(
        //     queueName: "sensors_queue.energy",
        //     channelProvider: sp.GetRequiredService<IRabbitMqChannelProvider>(),
        //     sp));
        
        services.AddHostedService(sp => new RabbitMqConsumer<AirQualityProcessingService>(
            queueName: "sensors_queue.air_quality",
            channelProvider: sp.GetRequiredService<IRabbitMqChannelProvider>(),
            sp));
        
        services.AddSingleton<IRabbitMqConfigProvider, RabbitMqConfigProvider>();
        services.AddSingleton<IRabbitMqChannelProvider, RabbitMqChannelProvider>();

        return services;
    }

    private static IServiceCollection AddDatabaseDependencies(this IServiceCollection services)
    {
        services.AddSingleton<IClickHouseConfigProvider, ClickHouseConfigProvider>();
        services.AddScoped<IClickHouseConnectionFactory, ClickHouseConnectionFactory>();
        services.AddScoped<IClickHouseInitializer, ClickHouseInitializer>();

        services.AddScoped<IAirQualityRepository, AirQualityRepository>();
        services.AddScoped<IMotionRepository, MotionRepository>();
        services.AddScoped<IEnergyRepository, EnergyRepository>();
        
        return services;
    }
}
