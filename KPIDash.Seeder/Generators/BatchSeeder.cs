using Dapper;
using KPIDash.Data;

namespace KPIDash.Seeder.Generators;

public class BatchSeeder(DbConnectionFactory factory, Random rng)
{
    private const double TargetDumpTemp = 120.0;

    public void Seed(
        Dictionary<int, List<StateWindow>> timelines,
        List<TimeLogSeeder.ShiftAssignment> shiftAssignments,
        List<ProductionScheduleSeeder.ScheduleRun> schedule,
        DateTime to)
    {
        using var conn = factory.Create();
        conn.Open();
        int total = 0;

        var mixers = conn.Query<(int EquipmentId, string Name)>(
            "SELECT EquipmentId, Name FROM Equipment WHERE Type = 'InternalMixer' ORDER BY DisplayOrder")
            .ToList();

        using var tx = conn.BeginTransaction();

        var dailyCounts = new Dictionary<string, int>();

        foreach (var (mixerId, mixerName) in mixers)
        {
            if (!timelines.TryGetValue(mixerId, out var timeline)) continue;

            var line = mixerName.StartsWith("Line 1") ? "Line 1" : "Line 2";
            var lineSchedule = schedule
                .Where(r => r.Line == line)
                .OrderBy(r => r.ScheduledStart)
                .ToList();

            foreach (var run in lineSchedule)
            {
                var cursor = run.ScheduledStart;
                var runEnd = run.ScheduledEnd < to ? run.ScheduledEnd : to;

                while (cursor < runEnd)
                {
                    // Skip any active Down period — this is the organic downtime impact on batch count
                    var down = timeline.FirstOrDefault(w =>
                        w.Status == "Down" && w.Start <= cursor && w.End > cursor);
                    if (down != null)
                    {
                        cursor = down.End < runEnd ? down.End : runEnd;
                        continue;
                    }

                    var batchMinutes = rng.Next(8, 13);
                    var batchEnd = cursor.AddMinutes(batchMinutes);
                    if (batchEnd > runEnd) break;

                    var operatorId = GetActiveOperator(cursor, line, shiftAssignments);
                    if (operatorId == 0) { cursor = batchEnd; continue; }

                    var dateKey = cursor.ToString("yyyyMMdd");
                    dailyCounts.TryAdd(dateKey, 0);
                    dailyCounts[dateKey]++;

                    var batchNumber = $"B-{dateKey}-{dailyCounts[dateKey]:D3}";
                    var (status, dumpTemp, completedAt) = AssignBatchOutcome(batchEnd, to);

                    conn.Execute("""
                        INSERT INTO Batches
                            (BatchNumber, StartedAt, CompletedAt, DumpTemperature, TargetDumpTemp,
                             Status, OperatorId, Line, CompoundCode)
                        VALUES
                            (@BatchNumber, @StartedAt, @CompletedAt, @DumpTemp, @TargetDumpTemp,
                             @Status, @OperatorId, @Line, @CompoundCode)
                        """,
                        new
                        {
                            BatchNumber  = batchNumber,
                            StartedAt    = cursor.ToString("o"),
                            CompletedAt  = completedAt?.ToString("o"),
                            DumpTemp     = dumpTemp,
                            TargetDumpTemp,
                            Status       = status,
                            OperatorId   = operatorId,
                            Line         = line,
                            CompoundCode = run.CompoundCode,
                        }, tx);

                    total++;
                    cursor = batchEnd;
                }
            }
        }

        tx.Commit();
        Console.WriteLine($"  Batches: {total} rows");
    }

    private (string Status, double? DumpTemp, DateTime? CompletedAt) AssignBatchOutcome(DateTime completedAt, DateTime now)
    {
        if (completedAt > now.AddHours(-12) && rng.NextDouble() < 0.02)
            return ("InProgress", null, null);

        var roll = rng.NextDouble();
        if (roll < 0.05)
        {
            var offset = rng.NextDouble() < 0.5
                ? -(10.0 + rng.NextDouble() * 15.0)
                : 10.0 + rng.NextDouble() * 15.0;
            return ("Rejected", Math.Round(TargetDumpTemp + offset, 1), completedAt);
        }

        var temp = Math.Round(NextGaussian(TargetDumpTemp, 5.0), 1);
        temp = Math.Max(TargetDumpTemp - 9.9, Math.Min(TargetDumpTemp + 9.9, temp));
        return ("Complete", temp, completedAt);
    }

    private int GetActiveOperator(DateTime time, string line, List<TimeLogSeeder.ShiftAssignment> assignments)
    {
        var (shiftDate, shiftName) = GetShiftInfo(time);

        var assignment = assignments.FirstOrDefault(
            a => a.ShiftDate == shiftDate && a.Shift == shiftName && a.Line == line);

        if (assignment == null || assignment.OperatorIds.Count == 0) return 0;
        return assignment.OperatorIds[rng.Next(assignment.OperatorIds.Count)];
    }

    // Night shift starts at 22:00 and is stored under the date it began.
    // Batches at 00:00–05:59 belong to the previous day's Night shift.
    private static (string ShiftDate, string ShiftName) GetShiftInfo(DateTime time) =>
        time.Hour switch
        {
            >= 6 and < 14  => (time.ToString("yyyy-MM-dd"), "Day"),
            >= 14 and < 22 => (time.ToString("yyyy-MM-dd"), "Afternoon"),
            < 6            => (time.AddDays(-1).ToString("yyyy-MM-dd"), "Night"),
            _              => (time.ToString("yyyy-MM-dd"), "Night"),
        };

    private double NextGaussian(double mean, double stdDev)
    {
        var u1 = 1.0 - rng.NextDouble();
        var u2 = 1.0 - rng.NextDouble();
        var z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stdDev * z;
    }
}
