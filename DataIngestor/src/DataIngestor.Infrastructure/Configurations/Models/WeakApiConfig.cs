namespace DataIngestor.Infrastructure.Configurations.Models;

public class WeakApiConfig
{
    public const string SectionName = "WeakApi";

    public string MetersUrl { get; set; } = string.Empty;

    public Dictionary<string, string> Headers { get; set; } = new();
}
