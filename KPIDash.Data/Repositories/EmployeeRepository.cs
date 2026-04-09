using Dapper;
using KPIDash.Data.Models;

namespace KPIDash.Data.Repositories;

public class EmployeeRepository(DbConnectionFactory connectionFactory) : IEmployeeRepository
{
    public async Task<IEnumerable<Employee>> GetAllAsync()
    {
        using var connection = connectionFactory.Create();
        return await connection.QueryAsync<Employee>(
            "SELECT * FROM Employees ORDER BY LastName, FirstName");
    }

    public async Task<Employee?> GetByIdAsync(int employeeId)
    {
        using var connection = connectionFactory.Create();
        return await connection.QuerySingleOrDefaultAsync<Employee>(
            "SELECT * FROM Employees WHERE EmployeeId = @EmployeeId",
            new { EmployeeId = employeeId });
    }
}
