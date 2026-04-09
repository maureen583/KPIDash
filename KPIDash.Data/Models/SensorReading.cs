namespace KPIDash.Data.Models;

public class SensorReading
{
    public int ReadingId { get; set; }
    public int SensorId { get; set; }
    public DateTime RecordedAt { get; set; }
    public double Value { get; set; }
}
