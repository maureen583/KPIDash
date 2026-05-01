using Dapper;
using KPIDash.Data.Models;

namespace KPIDash.Data.Repositories;

public class TimeLogRepository(DbConnectionFactory connectionFactory) : ITimeLogRepository
{
    public async Task<IEnumerable<TimeLog>> GetByShiftDateAsync(string shiftDate)
    {
        using var connection = connectionFactory.Create();
        return await connection.QueryAsync<TimeLog>(
            "SELECT * FROM TimeLog WHERE ShiftDate = @ShiftDate ORDER BY ClockIn",
            new { ShiftDate = shiftDate });
    }

    public async Task<IEnumerable<TimeLog>> GetByShiftAsync(string shiftDate, string shift, string line)
    {
        using var connection = connectionFactory.Create();
        return await connection.QueryAsync<TimeLog>(
            """
            SELECT * FROM TimeLog
            WHERE ShiftDate = @ShiftDate AND Shift = @Shift AND Line = @Line
            ORDER BY ClockIn
            """,
            new { ShiftDate = shiftDate, Shift = shift, Line = line });
    }

    public async Task<IEnumerable<TimeLog>> GetActiveAsync()
    {
        using var connection = connectionFactory.Create();
        return await connection.QueryAsync<TimeLog>(
            "SELECT * FROM TimeLog WHERE ClockOut IS NULL ORDER BY ClockIn");
    }

    public async Task<IEnumerable<TimeLogEntry>> GetByShiftDateWithEmployeesAsync(string shiftDate)
    {
        using var connection = connectionFactory.Create();
        return await connection.QueryAsync<TimeLogEntry>(
            """
            SELECT tl.TimeLogId, tl.EmployeeId,
                   e.FirstName || ' ' || e.LastName AS EmployeeName,
                   e.Role,
                   tl.ClockIn, tl.ClockOut, tl.ShiftDate, tl.Shift, tl.Line
            FROM TimeLog tl
            JOIN Employees e ON e.EmployeeId = tl.EmployeeId
            WHERE tl.ShiftDate = @ShiftDate
            ORDER BY
                CASE tl.Shift WHEN 'Day' THEN 1 WHEN 'Afternoon' THEN 2 WHEN 'Night' THEN 3 END,
                tl.Line,
                tl.ClockIn
            """,
            new { ShiftDate = shiftDate });
    }
}
