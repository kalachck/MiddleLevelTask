using DataIngestor.Infrastructure.Configurations.Models;
using Microsoft.Extensions.Options;

namespace DataIngestor.Infrastructure.Configurations.Providers;

public interface IWeakApiConfigProvider
{
    string GetMetersUrl();

    Dictionary<string, string> GetHeaders();
}

public class WeakApiConfigProvider : IWeakApiConfigProvider
{
    private readonly WeakApiConfig _config;
    
    public WeakApiConfigProvider(IOptions<WeakApiConfig> options)
    {
        _config = options.Value;
    }
    
    public string GetMetersUrl() 
        => _config.MetersUrl;

    public Dictionary<string, string> GetHeaders() 
        => _config.Headers;
}
