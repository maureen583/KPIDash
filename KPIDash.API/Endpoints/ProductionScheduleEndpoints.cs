using KPIDash.Data.Repositories;

namespace KPIDash.API.Endpoints;

public static class ProductionScheduleEndpoints
{
    public static void MapProductionScheduleEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/schedule").WithTags("Production Schedule");

        group.MapGet("/{shiftDate}/{shift}", async (string shiftDate, string shift, IProductionScheduleRepository repo) =>
            await repo.GetByShiftAsync(shiftDate, shift));
    }
}
