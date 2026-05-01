using KPIDash.Data.Models;

namespace KPIDash.Data.Repositories;

public interface IKpiRepository
{
    Task<KpiSummary?> GetAsync(string shiftDate, string shift, string line);
}
