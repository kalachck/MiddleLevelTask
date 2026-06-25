using System.Text;
using DataProcessor.Infrastructure.RabbitMq.Providers;
using RabbitMQ.Client;

namespace DataProcessor.Infrastructure.RabbitMq;

internal static class MessageRetry
{
    public static int GetAttempt(IReadOnlyBasicProperties properties)
    {
        if (properties.Headers is null
            || !properties.Headers.TryGetValue(RabbitMqConfig.AttemptHeader, out var raw)
            || raw is null)
        {
            return 0;
        }

        return raw switch
        {
            int value => value,
            long value => (int)value,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed) => parsed,
            _ => 0,
        };
    }

    public static BasicProperties CreateRetryProperties(IReadOnlyBasicProperties source, int nextAttempt)
    {
        var headers = source.Headers is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(source.Headers);

        headers[RabbitMqConfig.AttemptHeader] = nextAttempt;

        return new BasicProperties
        {
            Persistent = true,
            ContentType = source.ContentType,
            Headers = headers,
        };
    }
}
