namespace KPIDash.UI.Models;

public class Batch
{
    public int BatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public double? DumpTemperature { get; set; }
    public double TargetDumpTemp { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Line { get; set; } = string.Empty;
    public string CompoundCode { get; set; } = string.Empty;

    public double? CycleMinutes => CompletedAt.HasValue
        ? (CompletedAt.Value - StartedAt).TotalMinutes
        : null;
}
