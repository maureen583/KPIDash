using KPIDash.Data.Repositories;

namespace KPIDash.API.Endpoints;

public static class SensorEndpoints
{
    public static void MapSensorEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api").WithTags("Sensors");

        group.MapGet("/equipment/{id:int}/sensors", async (int id, ISensorRepository repo) =>
            await repo.GetByEquipmentIdAsync(id));

        group.MapGet("/sensors/{id:int}/readings/latest", async (int id, ISensorRepository repo) =>
        {
            var reading = await repo.GetLatestReadingAsync(id);
            return reading is null ? Results.NotFound() : Results.Ok(reading);
        });

        group.MapGet("/sensors/{id:int}/readings", async (int id, DateTime from, DateTime to, ISensorRepository repo) =>
            await repo.GetReadingsAsync(id, from, to));
    }
}
