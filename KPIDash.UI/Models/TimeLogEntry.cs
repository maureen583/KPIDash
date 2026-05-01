namespace KPIDash.UI.Models;

public class TimeLogEntry
{
    public int TimeLogId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public string ShiftDate { get; set; } = string.Empty;
    public string Shift { get; set; } = string.Empty;
    public string Line { get; set; } = string.Empty;
}
