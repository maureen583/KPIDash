using Dapper;
using KPIDash.Data;

namespace KPIDash.Seeder.Generators;

public class BatchSeeder(DbConnectionFactory factory, Random rng)
{
    private const double TargetDumpTemp = 120.0;

    public void Seed(
        Dictionary<int, List<StateWindow>> timelines,
        List<TimeLogSeeder.ShiftAssignment> shiftAssignments,
        DateTime to)
    {
        using var conn = factory.Create();
        int total = 0;

        // Process both Banbury mixers (InternalMixer type, DisplayOrder 3 and 4)
        var mixerIds = conn.Query<int>(
            "SELECT EquipmentId FROM Equipment WHERE Type = 'InternalMixer' ORDER BY DisplayOrder")
            .ToList();

        // Shared daily counter across both lines so batch numbers don't collide
        var dailyCounts = new Dictionary<string, int>();

        foreach (var mixerId in mixerIds)
        {
            if (!timelines.TryGetValue(mixerId, out var timeline)) continue;

            var runningWindows = timeline.Where(w => w.Status == "Running").ToList();

            foreach (var window in runningWindows)
            {
                var cursor = window.Start;

                while (cursor < window.End)
                {
                    var batchMinutes = rng.Next(8, 13);
                    var batchEnd = cursor.AddMinutes(batchMinutes);
                    if (batchEnd > window.End) break;

                    var dateKey = cursor.ToString("yyyyMMdd");
                    dailyCounts.TryAdd(dateKey, 0);
                    dailyCounts[dateKey]++;

                    var batchNumber = $"B-{dateKey}-{dailyCounts[dateKey]:D3}";
                    var operatorId = GetActiveOperator(cursor, shiftAssignments);
                    if (operatorId == 0) { cursor = batchEnd; continue; }

                    var (status, dumpTemp, completedAt) = AssignBatchOutcome(batchEnd, to);

                    conn.Execute("""
                        INSERT INTO Batches
                            (BatchNumber, StartedAt, CompletedAt, DumpTemperature, TargetDumpTemp, Status, OperatorId)
                        VALUES
                            (@BatchNumber, @StartedAt, @CompletedAt, @DumpTemp, @TargetDumpTemp, @Status, @OperatorId)
                        """,
                        new
                        {
                            BatchNumber = batchNumber,
                            StartedAt = cursor.ToString("o"),
                            CompletedAt = completedAt?.ToString("o"),
                            DumpTemp = dumpTemp,
                            TargetDumpTemp = TargetDumpTemp,
                            Status = status,
                            OperatorId = operatorId
                        });

                    total++;
                    cursor = batchEnd;
                }
            }
        }

        Console.WriteLine($"  Batches: {total} rows");
    }

    private (string Status, double? DumpTemp, DateTime? CompletedAt) AssignBatchOutcome(DateTime completedAt, DateTime now)
    {
        // Most recent batch of today may still be in progress
        if (completedAt > now.AddHours(-12) && rng.NextDouble() < 0.02)
            return ("InProgress", null, null);

        var roll = rng.NextDouble();
        if (roll < 0.05)
        {
            // Rejected: temp out of range by more than 10°C
            var offset = rng.NextDouble() < 0.5
                ? -(10.0 + rng.NextDouble() * 15.0)
                : 10.0 + rng.NextDouble() * 15.0;
            return ("Rejected", Math.Round(TargetDumpTemp + offset, 1), completedAt);
        }

        // Complete: normally distributed around target ±5
        var temp = Math.Round(NextGaussian(TargetDumpTemp, 5.0), 1);
        temp = Math.Max(TargetDumpTemp - 9.9, Math.Min(TargetDumpTemp + 9.9, temp));
        return ("Complete", temp, completedAt);
    }

    private int GetActiveOperator(DateTime time, List<TimeLogSeeder.ShiftAssignment> assignments)
    {
        var shiftDate = time.ToString("yyyy-MM-dd");
        var shiftName = GetShiftName(time);

        var assignment = assignments.FirstOrDefault(
            a => a.ShiftDate == shiftDate && a.Shift == shiftName);

        if (assignment == null || assignment.OperatorIds.Count == 0) return 0;
        return assignment.OperatorIds[rng.Next(assignment.OperatorIds.Count)];
    }

    private static string GetShiftName(DateTime time)
    {
        return time.Hour switch
        {
            >= 6 and < 14  => "Day",
            >= 14 and < 22 => "Afternoon",
            _              => "Night"
        };
    }

    private double NextGaussian(double mean, double stdDev)
    {
        var u1 = 1.0 - rng.NextDouble();
        var u2 = 1.0 - rng.NextDouble();
        var z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stdDev * z;
    }
}
