using KPIDash.Data.Repositories;

namespace KPIDash.API.Endpoints;

public static class BatchEndpoints
{
    public static void MapBatchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/batches").WithTags("Batches");

        group.MapGet("/today", async (IBatchRepository repo) =>
            await repo.GetTodaysAsync());

        group.MapGet("/last", async (IBatchRepository repo) =>
        {
            var batch = await repo.GetLastAsync();
            return batch is null ? Results.NotFound() : Results.Ok(batch);
        });

        group.MapGet("/", async (DateTime from, DateTime to, IBatchRepository repo) =>
            await repo.GetByPeriodAsync(from, to));

        group.MapGet("/{id:int}", async (int id, IBatchRepository repo) =>
        {
            var batch = await repo.GetByIdAsync(id);
            return batch is null ? Results.NotFound() : Results.Ok(batch);
        });
    }
}
