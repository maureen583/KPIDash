using KPIDash.Data.Repositories;

namespace KPIDash.API.Endpoints;

public static class EquipmentEndpoints
{
    public static void MapEquipmentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/equipment").WithTags("Equipment");

        group.MapGet("/", async (IEquipmentRepository repo) =>
            await repo.GetAllAsync());

        group.MapGet("/{id:int}", async (int id, IEquipmentRepository repo) =>
        {
            var item = await repo.GetByIdAsync(id);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });
    }
}
