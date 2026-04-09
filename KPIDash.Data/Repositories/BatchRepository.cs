using Dapper;
using KPIDash.Data.Models;

namespace KPIDash.Data.Repositories;

public class BatchRepository(DbConnectionFactory connectionFactory) : IBatchRepository
{
    public async Task<IEnumerable<Batch>> GetTodaysAsync()
    {
        using var connection = connectionFactory.Create();
        var today = DateTime.UtcNow.Date.ToString("o");
        var tomorrow = DateTime.UtcNow.Date.AddDays(1).ToString("o");
        return await connection.QueryAsync<Batch>(
            """
            SELECT * FROM Batches
            WHERE StartedAt >= @Today AND StartedAt < @Tomorrow
            ORDER BY StartedAt DESC
            """,
            new { Today = today, Tomorrow = tomorrow });
    }

    public async Task<IEnumerable<Batch>> GetByPeriodAsync(DateTime from, DateTime to)
    {
        using var connection = connectionFactory.Create();
        return await connection.QueryAsync<Batch>(
            """
            SELECT * FROM Batches
            WHERE StartedAt >= @From AND StartedAt <= @To
            ORDER BY StartedAt DESC
            """,
            new { From = from.ToString("o"), To = to.ToString("o") });
    }

    public async Task<Batch?> GetLastAsync()
    {
        using var connection = connectionFactory.Create();
        return await connection.QuerySingleOrDefaultAsync<Batch>(
            "SELECT * FROM Batches ORDER BY StartedAt DESC LIMIT 1");
    }

    public async Task<Batch?> GetByIdAsync(int batchId)
    {
        using var connection = connectionFactory.Create();
        return await connection.QuerySingleOrDefaultAsync<Batch>(
            "SELECT * FROM Batches WHERE BatchId = @BatchId",
            new { BatchId = batchId });
    }
}
