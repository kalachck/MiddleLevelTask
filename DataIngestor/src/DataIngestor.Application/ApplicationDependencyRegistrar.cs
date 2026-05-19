using DataIngestor.Application.Configurations.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DataIngestor.Application;

public static class DependencyRegistrar
{
    public static void AddApplicationDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<IngestionOptions>(configuration.GetSection(IngestionOptions.SectionName));
        services.AddHostedService<IngestionWorker>();
    }
}
