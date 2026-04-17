namespace KPIDash.UI.Models;

public class EquipmentStatus
{
    public int StatusId { get; set; }
    public int EquipmentId { get; set; }
    public DateTime RecordedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
