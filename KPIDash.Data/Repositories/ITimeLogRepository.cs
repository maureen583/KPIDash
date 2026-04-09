using KPIDash.Data.Models;

namespace KPIDash.Data.Repositories;

public interface ITimeLogRepository
{
    Task<IEnumerable<TimeLog>> GetByShiftDateAsync(string shiftDate);
    Task<IEnumerable<TimeLog>> GetActiveAsync();
}
