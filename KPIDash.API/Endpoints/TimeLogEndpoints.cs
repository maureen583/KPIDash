using KPIDash.Data.Repositories;

namespace KPIDash.API.Endpoints;

public static class TimeLogEndpoints
{
    public static void MapTimeLogEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/timelog").WithTags("Time Log");

        group.MapGet("/active", async (ITimeLogRepository repo) =>
            await repo.GetActiveAsync());

        group.MapGet("/{shiftDate}", async (string shiftDate, ITimeLogRepository repo) =>
            await repo.GetByShiftDateAsync(shiftDate));
    }
}
