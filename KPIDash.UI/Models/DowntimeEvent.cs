namespace KPIDash.UI.Models;

public class DowntimeEvent
{
    public int DowntimeId { get; set; }
    public int EquipmentId { get; set; }
    public string EquipmentName { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public double? DurationMinutes { get; set; }
    public string Reason { get; set; } = "";
    public string Category { get; set; } = "";
}
