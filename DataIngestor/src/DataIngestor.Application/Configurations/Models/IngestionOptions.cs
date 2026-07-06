namespace DataIngestor.Application.Configurations.Models;

public class IngestionOptions
{
    public const string SectionName = "Ingestion";

    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);
}
