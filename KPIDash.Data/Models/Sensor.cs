namespace KPIDash.Data.Models;

public class Sensor
{
    public int SensorId { get; set; }
    public int EquipmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public double MinNormal { get; set; }
    public double MaxNormal { get; set; }
    public bool IsStatusSensor { get; set; }
}
