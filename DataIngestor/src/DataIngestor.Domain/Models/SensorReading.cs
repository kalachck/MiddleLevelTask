using System.Text.Json.Serialization;

namespace DataIngestor.Domain.Models;

public record SensorReading(
    [property: JsonPropertyName("type")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWriting)]
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    SensorType Type,
    string Name,
    object Payload)
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public enum SensorType
{
    [JsonStringEnumMemberName("energy")]
    Energy,
    [JsonStringEnumMemberName("air_quality")]
    AirQuality,
    [JsonStringEnumMemberName("motion")]
    Motion,
    Unknown
}
