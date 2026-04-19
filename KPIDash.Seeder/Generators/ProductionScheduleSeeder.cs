using Dapper;
using KPIDash.Data;

namespace KPIDash.Seeder.Generators;

public class ProductionScheduleSeeder(DbConnectionFactory factory, Random rng)
{
    public record ScheduleRun(string Line, DateTime ScheduledStart, DateTime ScheduledEnd, string CompoundCode);

    private static readonly (string Code, string Name)[] Compounds =
    [
        ("NR-100",  "Natural Rubber Base"),
        ("SBR-200", "Styrene Butadiene General"),
        ("EPDM-300","EPDM Weather Seal"),
        ("NBR-400", "Nitrile Oil Resistant"),
        ("CR-500",  "Chloroprene Adhesive"),
        ("BR-600",  "Butadiene High Resilience"),
    ];

    // (shift, scheduledHours, plannedOperators)
    // Night is 6 scheduled hours (22:00–04:00), not 8
    private static readonly (string Shift, int StartHour, int ScheduledHours, int PlannedOps)[] Shifts =
    [
        ("Day",       6,  8, 5),
        ("Afternoon", 14, 8, 4),
        ("Night",     22, 6, 3),
    ];

    public List<ScheduleRun> Seed(DateTime from, DateTime to)
    {
        var allRuns = new List<ScheduleRun>();
        using var conn = factory.Create();
        int total = 0;

        // Track last compound per (line+shift) key to avoid consecutive repeats
        var lastCompound = new Dictionary<string, string>();

        for (var day = from.Date; day < to.Date; day = day.AddDays(1))
        {
            foreach (var (shift, startHour, scheduledHours, plannedOps) in Shifts)
            {
                var shiftDate = day.ToString("yyyy-MM-dd");
                var scheduledStart = day.AddHours(startHour);

                // Night: scheduled window ends at 04:00 the next day
                var scheduledEnd = shift == "Night"
                    ? day.AddDays(1).AddHours(4)
                    : day.AddHours(startHour + scheduledHours);

                var totalMinutes = (int)(scheduledEnd - scheduledStart).TotalMinutes;

                foreach (var line in new[] { "Line 1", "Line 2" })
                {
                    var runCount = rng.Next(2, 4); // 2 or 3 compound runs
                    var splits = GenerateSplits(runCount, totalMinutes);
                    var cursor = scheduledStart;
                    var lastKey = $"{line}|{shift}";

                    foreach (var durationMin in splits)
                    {
                        var runEnd = cursor.AddMinutes(durationMin);
                        var compound = PickCompound(lastCompound.GetValueOrDefault(lastKey));
                        lastCompound[lastKey] = compound.Code;

                        var targetBatches = (int)Math.Round(durationMin / 10.0);

                        conn.Execute("""
                            INSERT INTO ProductionSchedule
                                (ShiftDate, Shift, Line, CompoundCode, CompoundName,
                                 ScheduledStart, ScheduledEnd, TargetBatches, PlannedOperators)
                            VALUES
                                (@ShiftDate, @Shift, @Line, @CompoundCode, @CompoundName,
                                 @ScheduledStart, @ScheduledEnd, @TargetBatches, @PlannedOperators)
                            """,
                            new
                            {
                                ShiftDate        = shiftDate,
                                Shift            = shift,
                                Line             = line,
                                CompoundCode     = compound.Code,
                                CompoundName     = compound.Name,
                                ScheduledStart   = cursor.ToString("o"),
                                ScheduledEnd     = runEnd.ToString("o"),
                                TargetBatches    = targetBatches,
                                PlannedOperators = plannedOps,
                            });

                        allRuns.Add(new ScheduleRun(line, cursor, runEnd, compound.Code));
                        cursor = runEnd;
                        total++;
                    }
                }
            }
        }

        Console.WriteLine($"  ProductionSchedule: {total} rows");
        return allRuns;
    }

    // Splits totalMinutes into `count` segments with random proportions (weights in [1, 2.5])
    private int[] GenerateSplits(int count, int totalMinutes)
    {
        var weights = Enumerable.Range(0, count)
            .Select(_ => 1.0 + rng.NextDouble() * 1.5)
            .ToArray();
        var weightSum = weights.Sum();

        var splits = new int[count];
        int allocated = 0;
        for (int i = 0; i < count - 1; i++)
        {
            splits[i] = (int)Math.Round(weights[i] / weightSum * totalMinutes);
            allocated += splits[i];
        }
        splits[^1] = totalMinutes - allocated;
        return splits;
    }

    private (string Code, string Name) PickCompound(string? lastCode)
    {
        var available = Compounds.Where(c => c.Code != lastCode).ToArray();
        return available[rng.Next(available.Length)];
    }
}
