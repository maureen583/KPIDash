using KPIDash.Data.Repositories;

namespace KPIDash.API.Endpoints;

public static class EmployeeEndpoints
{
    public static void MapEmployeeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/employees").WithTags("Employees");

        group.MapGet("/", async (IEmployeeRepository repo) =>
            await repo.GetAllAsync());

        group.MapGet("/{id:int}", async (int id, IEmployeeRepository repo) =>
        {
            var employee = await repo.GetByIdAsync(id);
            return employee is null ? Results.NotFound() : Results.Ok(employee);
        });
    }
}
