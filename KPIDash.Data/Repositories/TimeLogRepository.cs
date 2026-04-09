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

    public async Task<IEnumerable<TimeLog>> GetActiveAsync()
    {
        using var connection = connectionFactory.Create();
        return await connection.QueryAsync<TimeLog>(
            "SELECT * FROM TimeLog WHERE ClockOut IS NULL ORDER BY ClockIn");
    }
}
