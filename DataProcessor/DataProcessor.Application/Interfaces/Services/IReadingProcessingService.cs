namespace DataProcessor.Application.Interfaces.Services;

public interface IReadingProcessingService
{
    Task ProcessReading(string jsonData, CancellationToken ct);
}
