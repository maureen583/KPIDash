namespace KPIDash.Data.Models;

public class TimeLog
{
    public int TimeLogId { get; set; }
    public int EmployeeId { get; set; }
    public DateTime ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public string ShiftDate { get; set; } = string.Empty;
    public string Shift { get; set; } = string.Empty;
}
