using DataIngestor.Application.Configurations.Models;
using DataIngestor.Application.Metrics;
using DataIngestor.Domain.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataIngestor.Application;

public static class DependencyRegistrar
{
    public static void AddApplicationDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IngestionMetrics>();
        services.Configure<IngestionOptions>(configuration.GetSection(IngestionOptions.SectionName));

        services.AddHostedService<IngestionWorker>(sp => new IngestionWorker(
            sp.GetRequiredService<IWeakApiClient>(),
            sp.GetRequiredService<IMessagePublisher>(),
            sp.GetRequiredService<ILogger<IngestionWorker>>(),
            sp.GetRequiredService<IngestionMetrics>(),
            sp.GetRequiredService<IOptions<IngestionOptions>>()));
    }
}
