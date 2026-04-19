namespace KPIDash.Data.Models;

public class ProductionSchedule
{
    public int ScheduleId { get; set; }
    public string ShiftDate { get; set; } = string.Empty;
    public string Shift { get; set; } = string.Empty;
    public string Line { get; set; } = string.Empty;
    public string CompoundCode { get; set; } = string.Empty;
    public string CompoundName { get; set; } = string.Empty;
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }
    public int TargetBatches { get; set; }
    public int PlannedOperators { get; set; }
}
