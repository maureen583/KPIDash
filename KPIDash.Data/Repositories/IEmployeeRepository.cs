using KPIDash.Data.Models;

namespace KPIDash.Data.Repositories;

public interface IEmployeeRepository
{
    Task<IEnumerable<Employee>> GetAllAsync();
    Task<Employee?> GetByIdAsync(int employeeId);
}
