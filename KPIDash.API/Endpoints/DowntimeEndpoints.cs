using KPIDash.Data.Repositories;

namespace KPIDash.API.Endpoints;

public static class DowntimeEndpoints
{
    public static void MapDowntimeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api").WithTags("Downtime");

        group.MapGet("/downtime", async (IDowntimeRepository repo, int days = 7) =>
            await repo.GetRecentAsync(days));

        group.MapGet("/equipment/{id:int}/downtime", async (int id, IDowntimeRepository repo) =>
            await repo.GetByEquipmentAsync(id));

        group.MapGet("/downtime/shift", async (DateTime from, DateTime to, IDowntimeRepository repo) =>
            await repo.GetByShiftAsync(from, to));

        group.MapPatch("/downtime/{id:int}/category", async (int id, UpdateCategoryRequest req, IDowntimeRepository repo) =>
        {
            await repo.UpdateCategoryAsync(id, req.Category);
            return Results.NoContent();
        });
    }
}

record UpdateCategoryRequest(string Category);
