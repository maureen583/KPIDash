namespace KPIDash.UI.Models;

public class ProductionSchedule
{
    public int ScheduleId { get; set; }
    public string ShiftDate { get; set; } = "";
    public string Shift { get; set; } = "";
    public string Line { get; set; } = "";
    public string CompoundCode { get; set; } = "";
    public string CompoundName { get; set; } = "";
    public string ScheduledStart { get; set; } = "";
    public string ScheduledEnd { get; set; } = "";
    public int TargetBatches { get; set; }
    public int PlannedOperators { get; set; }
}
