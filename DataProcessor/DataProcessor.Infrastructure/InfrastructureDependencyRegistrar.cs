using DataProcessor.Application.Interfaces.Repositories;
using DataProcessor.Application.Interfaces.Services;
using DataProcessor.Application.Metrics;
using DataProcessor.Application.Services;
using DataProcessor.Infrastructure.ClickHouse;
using DataProcessor.Infrastructure.RabbitMq;
using DataProcessor.Infrastructure.RabbitMq.Providers;
using DataProcessor.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataProcessor.Infrastructure;

public static class InfrastructureDependencyRegistrar
{
    public static void AddInfrastructureDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RabbitMqConfig>(configuration.GetSection(RabbitMqConfig.SectionName));
        services.Configure<NotificationsRabbitMqConfig>(configuration.GetSection(NotificationsRabbitMqConfig.SectionName));

        services.AddRabbitMq();
        services.AddDatabaseDependencies();
    }

    public static void AddInfrastructureAppDependencies(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DataProcessor.Infrastructure");
        var databaseInitializer = scope.ServiceProvider.GetRequiredService<IClickHouseInitializer>();

        logger.LogInformation("Running ClickHouse schema initialization");
        databaseInitializer.InitializeAsync().GetAwaiter().GetResult();
        logger.LogInformation("ClickHouse schema initialization completed");
    }

    private static void AddRabbitMq(this IServiceCollection services)
    {
        services.AddSingleton<IRabbitMqConfigProvider, RabbitMqConfigProvider>();
        var maxRetries = services.BuildServiceProvider()
            .GetRequiredService<IRabbitMqConfigProvider>().GetRabbitMqConfig().MaxRetries;

        services.AddHostedService(sp => new RabbitMqConsumer<MotionProcessingService>(
            queueName: "sensors_queue.motion",
            maxRetries: maxRetries,
            channelProvider: sp.GetRequiredService<IRabbitMqChannelProvider>(),
            sp.GetRequiredService<DataProcessorMetrics>(),
            sp.GetRequiredService<ILogger<RabbitMqConsumer<MotionProcessingService>>>(),
            sp.GetRequiredService<MotionProcessingService>()));

        services.AddHostedService(sp => new RabbitMqConsumer<EnergyProcessingService>(
            queueName: "sensors_queue.energy",
            maxRetries: maxRetries,
            channelProvider: sp.GetRequiredService<IRabbitMqChannelProvider>(),
            sp.GetRequiredService<DataProcessorMetrics>(),
            sp.GetRequiredService<ILogger<RabbitMqConsumer<EnergyProcessingService>>>(),
            sp.GetRequiredService<EnergyProcessingService>()));

        services.AddHostedService(sp => new RabbitMqConsumer<AirQualityProcessingService>(
            queueName: "sensors_queue.air_quality",
            maxRetries: maxRetries,
            channelProvider: sp.GetRequiredService<IRabbitMqChannelProvider>(),
            sp.GetRequiredService<DataProcessorMetrics>(),
            sp.GetRequiredService<ILogger<RabbitMqConsumer<AirQualityProcessingService>>>(),
            sp.GetRequiredService<AirQualityProcessingService>()));

        services.AddSingleton<IRabbitMqChannelProvider, RabbitMqChannelProvider>();
        services.AddSingleton<ISensorNotificationService, RabbitMqNotificationPublisher>();
    }

    private static void AddDatabaseDependencies(this IServiceCollection services)
    {
        services.AddSingleton<IClickHouseConfigProvider, ClickHouseConfigProvider>();
        services.AddSingleton<IClickHouseConnectionFactory, ClickHouseConnectionFactory>();
        services.AddScoped<IClickHouseInitializer, ClickHouseInitializer>();

        services.AddSingleton<AirQualityRepository>();
        services.AddSingleton<IAirQualityRepository>(sp => sp.GetRequiredService<AirQualityRepository>());
        services.AddHostedService(sp => sp.GetRequiredService<AirQualityRepository>());

        services.AddSingleton<MotionRepository>();
        services.AddSingleton<IMotionRepository>(sp => sp.GetRequiredService<MotionRepository>());
        services.AddHostedService(sp => sp.GetRequiredService<MotionRepository>());

        services.AddSingleton<EnergyRepository>();
        services.AddSingleton<IEnergyRepository>(sp => sp.GetRequiredService<EnergyRepository>());
        services.AddHostedService(sp => sp.GetRequiredService<EnergyRepository>());
    }
}
