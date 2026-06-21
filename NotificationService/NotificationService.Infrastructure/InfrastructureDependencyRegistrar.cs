using NotificationService.Application.Dtos;
using NotificationService.Application.Metrics;
using NotificationService.Infrastructure.RabbitMq;
using NotificationService.Infrastructure.RabbitMq.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace NotificationService.Infrastructure;

public static class InfrastructureDependencyRegistrar
{
    private static readonly string[] NotificationTypes = ["energy", "motion", "air_quality"];

    public static void AddInfrastructureDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RabbitMqConfig>(configuration.GetSection(RabbitMqConfig.SectionName));
        services.Configure<NotificationsRabbitMqConfig>(configuration.GetSection(NotificationsRabbitMqConfig.SectionName));

        services.AddSingleton<NotificationMetrics>();

        services.AddSingleton<IRabbitMqConfigProvider, RabbitMqConfigProvider>();
        services.AddSingleton<IRabbitMqChannelProvider, RabbitMqChannelProvider>();

        services.AddHostedService(sp => new NotificationConsumer<EnergyReadingDto>(
            queueName: $"{configuration["Notifications:QueueName"]}.energy",
            notificationType: "energy",
            channelProvider: sp.GetRequiredService<IRabbitMqChannelProvider>(),
            serviceProvider: sp,
            metrics: sp.GetRequiredService<NotificationMetrics>(),
            logger: sp.GetRequiredService<ILogger<NotificationConsumer<EnergyReadingDto>>>()));

        services.AddHostedService(sp => new NotificationConsumer<MotionReadingDto>(
            queueName: $"{configuration["Notifications:QueueName"]}.motion",
            notificationType: "motion",
            channelProvider: sp.GetRequiredService<IRabbitMqChannelProvider>(),
            serviceProvider: sp,
            metrics: sp.GetRequiredService<NotificationMetrics>(),
            logger: sp.GetRequiredService<ILogger<NotificationConsumer<MotionReadingDto>>>()));

        services.AddHostedService(sp => new NotificationConsumer<AirQualityReadingDto>(
            queueName: $"{configuration["Notifications:QueueName"]}.air_quality",
            notificationType: "air_quality",
            channelProvider: sp.GetRequiredService<IRabbitMqChannelProvider>(),
            serviceProvider: sp,
            metrics: sp.GetRequiredService<NotificationMetrics>(),
            logger: sp.GetRequiredService<ILogger<NotificationConsumer<AirQualityReadingDto>>>()));
    }

    public static async Task InitializeRabbitMqAsync(this IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        var configProvider = serviceProvider.GetRequiredService<IRabbitMqConfigProvider>();
        var channelProvider = serviceProvider.GetRequiredService<IRabbitMqChannelProvider>();
        var notifications = configProvider.GetNotificationsConfig();
        var channel = await channelProvider.GetChannel(ct);

        await channel.ExchangeDeclareAsync(
            exchange: notifications.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: ct);

        foreach (var type in NotificationTypes)
        {
            var queueName = $"{notifications.QueueName}.{type}";
            var routingKey = $"{notifications.RoutingKeyPattern}{type}";

            await channel.QueueDeclareAsync(
                queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: ct);

            await channel.QueueBindAsync(
                queueName,
                notifications.ExchangeName,
                routingKey,
                cancellationToken: ct);
        }
    }
}
