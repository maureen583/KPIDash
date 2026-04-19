using Dapper;
using KPIDash.Data.Models;

namespace KPIDash.Data.Repositories;

public class ProductionScheduleRepository(DbConnectionFactory connectionFactory) : IProductionScheduleRepository
{
    public async Task<IEnumerable<ProductionSchedule>> GetByShiftAsync(string shiftDate, string shift)
    {
        using var connection = connectionFactory.Create();
        return await connection.QueryAsync<ProductionSchedule>(
            """
            SELECT * FROM ProductionSchedule
            WHERE ShiftDate = @ShiftDate AND Shift = @Shift
            ORDER BY Line, ScheduledStart
            """,
            new { ShiftDate = shiftDate, Shift = shift });
    }
}
