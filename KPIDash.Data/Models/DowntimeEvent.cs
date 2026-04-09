namespace KPIDash.Data.Models;

public class DowntimeEvent
{
    public int DowntimeId { get; set; }
    public int EquipmentId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public double? DurationMinutes { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
