using System.Text;
using DataProcessor.Application.Interfaces.Services;
using DataProcessor.Infrastructure.RabbitMq.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DataProcessor.Infrastructure.RabbitMq;

public class RabbitMqConsumer<TService> : BackgroundService 
    where TService : IReadingProcessingService
{
    private readonly string _queueName;
    private readonly IRabbitMqChannelProvider _channelProvider;
    private readonly IServiceProvider _serviceProvider;

    public RabbitMqConsumer(
        string queueName,
        IRabbitMqChannelProvider channelProvider,
        IServiceProvider serviceProvider)
    {
        _queueName = queueName;
        _channelProvider = channelProvider;
        _serviceProvider = serviceProvider;
    }
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var channel = await _channelProvider.GetChannel(ct);
        
        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                using var scope = _serviceProvider.CreateScope();
                var processingService = scope.ServiceProvider.GetRequiredKeyedService<IReadingProcessingService>(typeof(TService).Name);
                
                await processingService.ProcessReading(message, ct);

                await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
            }
            catch (Exception ex)
            {
                await channel.BasicNackAsync(ea.DeliveryTag, false, true, ct);
            }
        };

        await channel.BasicConsumeAsync(
            queue: _queueName, 
            autoAck: false, 
            consumer: consumer, 
            cancellationToken: ct);
    }
}
