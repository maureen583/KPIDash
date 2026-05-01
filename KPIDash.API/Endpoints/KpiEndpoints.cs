using KPIDash.Data.Repositories;

namespace KPIDash.API.Endpoints;

public static class KpiEndpoints
{
    public static void MapKpiEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/kpi").WithTags("KPI");

        group.MapGet("/{shiftDate}/{shift}/{line}", async (
            string shiftDate,
            string shift,
            string line,
            IKpiRepository repo) =>
        {
            var summary = await repo.GetAsync(shiftDate, shift, line);
            return summary is null ? Results.NotFound() : Results.Ok(summary);
        });
    }
}
