using Dapper;
using KPIDash.Data;

namespace KPIDash.Seeder.Generators;

public class DowntimeEventSeeder(DbConnectionFactory factory)
{
    private static readonly Dictionary<string, string> ReasonCategory = new()
    {
        ["BearingFailure"]  = "Mechanical",
        ["HydraulicLeak"]   = "Mechanical",
        ["DriveFailure"]    = "Mechanical",
        ["MotorOverload"]   = "Electrical",
        ["PowerOutage"]     = "Electrical",
        ["RubberStick"]     = "Process",
        ["TempOutOfRange"]  = "Process",
        ["CoolingFailure"]  = "Process",
        ["EmergencyStop"]   = "Safety",
        ["SafetyGuardOpen"] = "Safety",
    };

    public void Seed(Dictionary<int, List<StateWindow>> timelines)
    {
        using var conn = factory.Create();
        int total = 0;

        foreach (var (equipId, windows) in timelines)
        {
            var downWindows = windows.Where(w => w.Status == "Down").ToList();

            foreach (var window in downWindows)
            {
                var duration = (window.End - window.Start).TotalMinutes;
                var category = ReasonCategory.GetValueOrDefault(window.Reason, "");

                conn.Execute("""
                    INSERT INTO DowntimeEvents (EquipmentId, StartedAt, EndedAt, DurationMinutes, Reason, Category)
                    VALUES (@EquipmentId, @StartedAt, @EndedAt, @DurationMinutes, @Reason, @Category)
                    """,
                    new
                    {
                        EquipmentId     = equipId,
                        StartedAt       = window.Start.ToString("o"),
                        EndedAt         = window.End.ToString("o"),
                        DurationMinutes = Math.Round(duration, 2),
                        Reason          = window.Reason,
                        Category        = category
                    });
                total++;
            }
        }

        Console.WriteLine($"  DowntimeEvents: {total} rows");
    }
}
