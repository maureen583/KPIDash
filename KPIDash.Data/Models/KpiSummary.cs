namespace KPIDash.Data.Models;

public class KpiSummary
{
    public string Line { get; set; } = string.Empty;
    public string ShiftDate { get; set; } = string.Empty;
    public string Shift { get; set; } = string.Empty;

    // Raw inputs (exposed so the UI can render tooltip breakdowns)
    public double PlannedProductionMinutes { get; set; }
    public double DowntimeMinutes { get; set; }
    public double IdleMinutes { get; set; }
    public int TargetBatches { get; set; }
    public int ActualBatches { get; set; }
    public int GoodBatches { get; set; }
    public int TotalBatches { get; set; }
    public int PlannedOperators { get; set; }
    public int ActualOperators { get; set; }

    // Calculated KPIs (0–1 ratios, except BatchesPerOperator)
    public double Availability { get; set; }
    public double Performance { get; set; }
    public double Quality { get; set; }
    public double Oee { get; set; }
    public double BatchesPerOperator { get; set; }
    public double LabourEfficiency { get; set; }
}
