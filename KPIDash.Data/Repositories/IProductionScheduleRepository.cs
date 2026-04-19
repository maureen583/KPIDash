using KPIDash.Data.Models;

namespace KPIDash.Data.Repositories;

public interface IProductionScheduleRepository
{
    Task<IEnumerable<ProductionSchedule>> GetByShiftAsync(string shiftDate, string shift);
}
