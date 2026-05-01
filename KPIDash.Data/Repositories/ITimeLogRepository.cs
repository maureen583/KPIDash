using KPIDash.Data.Models;

namespace KPIDash.Data.Repositories;

public interface ITimeLogRepository
{
    Task<IEnumerable<TimeLog>> GetByShiftDateAsync(string shiftDate);
    Task<IEnumerable<TimeLog>> GetByShiftAsync(string shiftDate, string shift, string line);
    Task<IEnumerable<TimeLog>> GetActiveAsync();
    Task<IEnumerable<TimeLogEntry>> GetByShiftDateWithEmployeesAsync(string shiftDate);
}
