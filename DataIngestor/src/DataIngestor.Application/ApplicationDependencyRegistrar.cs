using Microsoft.Extensions.DependencyInjection;

namespace DataIngestor.Application;

public static class DependencyRegistrar
{
    public static void AddApplicationDependencies(this IServiceCollection services)
    {
        services.AddHostedService<IngestionWorker>();
    }
}
