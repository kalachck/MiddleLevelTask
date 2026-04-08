namespace DataProcessor.Application.Dtos;

public class AirQualityReadingDto
{
    public string Name { get; set; } = null!;
    public AirQualityPayload Payload { get; set; } = null!;
    public DateTime Timestamp { get; set; }
}

public class MotionReadingDto
{
    public string Name { get; set; } = null!;
    public MotionPayload Payload { get; set; } = null!;
    public DateTime Timestamp { get; set; }
}

public class EnergyReadingDto
{
    public string Name { get; set; } = null!;
    public EnergyPayload Payload { get; set; } = null!;
    public DateTime Timestamp { get; set; }
}

public class AirQualityPayload
{
    public int Co2 { get; set; }
    public int Pm25 { get; set; }
    public int Humidity { get; set; }
}

public class MotionPayload
{
    public bool MotionDetected { get; set; }
}

public class EnergyPayload
{
    public decimal Energy { get; set; }
}
