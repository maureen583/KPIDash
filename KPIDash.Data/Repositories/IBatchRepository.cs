using KPIDash.Data.Models;

namespace KPIDash.Data.Repositories;

public interface IBatchRepository
{
    Task<IEnumerable<Batch>> GetTodaysAsync();
    Task<IEnumerable<Batch>> GetByPeriodAsync(DateTime from, DateTime to);
    Task<Batch?> GetLastAsync();
    Task<Batch?> GetByIdAsync(int batchId);
}
