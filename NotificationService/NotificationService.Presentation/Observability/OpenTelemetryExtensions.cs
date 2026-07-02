using NotificationService.Application.Metrics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace NotificationService.Presentation.Observability;

public static class OpenTelemetryExtensions
{
    public static void AddObservability(this IServiceCollection services, string serviceName)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                .AddMeter(NotificationMetrics.MeterName)
                .AddPrometheusExporter());

        services.AddHealthChecks();
    }

    public static void UseObservability(this WebApplication app)
    {
        app.MapPrometheusScrapingEndpoint();
        app.MapHealthChecks("/health");
    }
}
