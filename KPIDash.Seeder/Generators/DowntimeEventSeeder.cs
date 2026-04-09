using Dapper;
using KPIDash.Data;

namespace KPIDash.Seeder.Generators;

public class DowntimeEventSeeder(DbConnectionFactory factory)
{
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

                conn.Execute("""
                    INSERT INTO DowntimeEvents (EquipmentId, StartedAt, EndedAt, DurationMinutes, Reason)
                    VALUES (@EquipmentId, @StartedAt, @EndedAt, @DurationMinutes, @Reason)
                    """,
                    new
                    {
                        EquipmentId = equipId,
                        StartedAt = window.Start.ToString("o"),
                        EndedAt = window.End.ToString("o"),
                        DurationMinutes = Math.Round(duration, 2),
                        Reason = window.Reason
                    });
                total++;
            }
        }

        Console.WriteLine($"  DowntimeEvents: {total} rows");
    }
}
