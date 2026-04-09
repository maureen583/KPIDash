namespace KPIDash.Data.Models;

public class Equipment
{
    public int EquipmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
