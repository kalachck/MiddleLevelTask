namespace DataIngestor.Application.Configurations.Models;

public class IngestionOptions
{
    public const string SectionName = "Ingestion";

    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(1);
}
