using KPIDash.Data.Repositories;

namespace KPIDash.API.Endpoints;

public static class EquipmentStatusEndpoints
{
    public static void MapEquipmentStatusEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/equipment").WithTags("Equipment Status");

        group.MapGet("/{id:int}/status", async (int id, IEquipmentStatusRepository repo) =>
        {
            var status = await repo.GetLatestAsync(id);
            return status is null ? Results.NotFound() : Results.Ok(status);
        });

        group.MapGet("/{id:int}/status/history", async (int id, DateTime from, DateTime to, IEquipmentStatusRepository repo) =>
            await repo.GetHistoryAsync(id, from, to));
    }
}
