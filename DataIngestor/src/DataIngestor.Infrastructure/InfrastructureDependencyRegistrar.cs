using DataIngestor.Domain.Abstractions;
using DataIngestor.Infrastructure.Configurations.Models;
using DataIngestor.Infrastructure.Configurations.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace DataIngestor.Infrastructure;

public static class InfrastructureDependencyRegistrar
{
    public static void AddInfrastructureDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpClient<IWeakApiClient, WeakApiClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddPolicyHandler((sp, _) => GetRetryPolicy(
            sp.GetRequiredService<ILoggerFactory>().CreateLogger("DataIngestor.WeakApi.HttpRetry")));

        services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

        services.Configure<RabbitMqConfig>(configuration.GetSection(RabbitMqConfig.SectionName));
        services.Configure<WeakApiConfig>(configuration.GetSection(WeakApiConfig.SectionName));

        services.AddSingleton<IRabbitMqConfigProvider, RabbitMqConfigProvider>();
        services.AddSingleton<IWeakApiConfigProvider, WeakApiConfigProvider>();
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TaskCanceledException>()
            .Or<HttpRequestException>()
            .Or<HttpIOException>()
            .WaitAndRetryAsync(
                5,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, _) =>
                {
                    var message = outcome.Exception is TaskCanceledException
                        ? "Request timeout exceeded (Timeout)"
                        : outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString();

                    logger.LogWarning(
                        "Weak API request failed (attempt {RetryAttempt}/5): {FailureReason}. Retrying in {RetryDelaySeconds:N1}s",
                        retryAttempt,
                        message,
                        timespan.TotalSeconds);
                });
    }
}
