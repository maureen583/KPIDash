using Dapper;
using KPIDash.Data;

namespace KPIDash.Seeder.Generators;

public class TimeLogSeeder(DbConnectionFactory factory, Random rng)
{
    private static readonly (string Shift, int StartHour, int EndHour)[] Shifts =
    [
        ("Day",       6,  14),
        ("Afternoon", 14, 22),
        ("Night",     22, 6),   // crosses midnight; ClockOut is next day
    ];

    // Per-line planned operator counts (Day 6+4, Afternoon 4+4, Night 4+2)
    private static readonly Dictionary<string, (int Line1, int Line2)> PlannedPerLine = new()
    {
        ["Day"]       = (6, 4),
        ["Afternoon"] = (4, 4),
        ["Night"]     = (4, 2),
    };

    public record ShiftAssignment(string ShiftDate, string Shift, string Line, List<int> OperatorIds);

    public List<ShiftAssignment> Seed(List<(int EmployeeId, string Role)> employees, DateTime from, DateTime to)
    {
        using var conn = factory.Create();

        var operators = employees
            .Where(e => e.Role is "General Operator" or "Mixers" or "Mill Man")
            .Select(e => e.EmployeeId)
            .ToList();

        var assignments = new List<ShiftAssignment>();
        int total = 0;

        for (var day = from.Date; day <= to.Date; day = day.AddDays(1))
        {
            foreach (var (shift, startHour, endHour) in Shifts)
            {
                var shiftDate    = day.ToString("yyyy-MM-dd");
                var clockInBase  = day.AddHours(startHour);
                var clockOutBase = shift == "Night"
                    ? day.AddDays(1).AddHours(endHour)
                    : day.AddHours(endHour);

                // Skip shifts that haven't started yet
                if (clockInBase > to) continue;

                var (planned1, planned2) = PlannedPerLine[shift];

                // Shuffle all operators and split into two per-line groups
                var shuffled = operators.OrderBy(_ => rng.Next()).ToList();
                // Actual counts vary at or below planned (never exceed planned)
                var count1 = Math.Clamp(planned1 + rng.Next(-1, 1), 1, shuffled.Count / 2 + 1);
                var count2 = Math.Clamp(planned2 + rng.Next(-1, 1), 1, shuffled.Count - count1);

                var line1Ops = shuffled.Take(count1).ToList();
                var line2Ops = shuffled.Skip(count1).Take(count2).ToList();

                foreach (var (line, lineOps) in new[] { ("Line 1", line1Ops), ("Line 2", line2Ops) })
                {
                    foreach (var empId in lineOps)
                    {
                        var clockIn  = clockInBase.AddMinutes(rng.Next(-15, 16));
                        // Only record ClockOut if the shift has ended
                        var clockOut = clockOutBase <= to
                            ? (DateTime?)clockOutBase.AddMinutes(rng.Next(-15, 16))
                            : null;

                        conn.Execute("""
                            INSERT INTO TimeLog (EmployeeId, ClockIn, ClockOut, ShiftDate, Shift, Line)
                            VALUES (@EmployeeId, @ClockIn, @ClockOut, @ShiftDate, @Shift, @Line)
                            """,
                            new
                            {
                                EmployeeId = empId,
                                ClockIn    = clockIn.ToString("o"),
                                ClockOut   = clockOut?.ToString("o"),
                                ShiftDate  = shiftDate,
                                Shift      = shift,
                                Line       = line,
                            });
                        total++;
                    }

                    assignments.Add(new ShiftAssignment(shiftDate, shift, line, lineOps));
                }
            }
        }

        Console.WriteLine($"  TimeLog: {total} rows");
        return assignments;
    }
}
