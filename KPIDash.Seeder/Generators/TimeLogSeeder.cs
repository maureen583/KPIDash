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

    public record ShiftAssignment(string ShiftDate, string Shift, List<int> OperatorIds);

    public List<ShiftAssignment> Seed(List<(int EmployeeId, string Role)> employees, DateTime from, DateTime to)
    {
        using var conn = factory.Create();

        var operators = employees
            .Where(e => e.Role is "General Operator" or "Mixers" or "Mill Man")
            .Select(e => e.EmployeeId)
            .ToList();

        var assignments = new List<ShiftAssignment>();
        int total = 0;

        for (var day = from.Date; day < to.Date; day = day.AddDays(1))
        {
            foreach (var (shift, startHour, endHour) in Shifts)
            {
                var shiftDate = day.ToString("yyyy-MM-dd");
                var clockInBase = day.AddHours(startHour);
                var clockOutBase = shift == "Night"
                    ? day.AddDays(1).AddHours(endHour)
                    : day.AddHours(endHour);

                // 3-5 operators per shift; heavier staffing on Day shift
                var count = shift == "Day" ? rng.Next(4, 6) : rng.Next(3, 5);
                var shuffled = operators.OrderBy(_ => rng.Next()).Take(count).ToList();

                foreach (var empId in shuffled)
                {
                    var clockIn = clockInBase.AddMinutes(rng.Next(-15, 16));
                    var clockOut = clockOutBase.AddMinutes(rng.Next(-15, 16));

                    conn.Execute("""
                        INSERT INTO TimeLog (EmployeeId, ClockIn, ClockOut, ShiftDate, Shift)
                        VALUES (@EmployeeId, @ClockIn, @ClockOut, @ShiftDate, @Shift)
                        """,
                        new
                        {
                            EmployeeId = empId,
                            ClockIn = clockIn.ToString("o"),
                            ClockOut = clockOut.ToString("o"),
                            ShiftDate = shiftDate,
                            Shift = shift
                        });
                    total++;
                }

                assignments.Add(new ShiftAssignment(shiftDate, shift, shuffled));
            }
        }

        Console.WriteLine($"  TimeLog: {total} rows");
        return assignments;
    }
}
