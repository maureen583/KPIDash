using Dapper;
using KPIDash.Data.Models;

namespace KPIDash.Data.Repositories;

public class DowntimeRepository(DbConnectionFactory connectionFactory) : IDowntimeRepository
{
    public async Task<IEnumerable<DowntimeEvent>> GetRecentAsync(int days = 7)
    {
        using var connection = connectionFactory.Create();
        var from = DateTime.UtcNow.AddDays(-days).ToString("o");
        return await connection.QueryAsync<DowntimeEvent>(
            "SELECT * FROM DowntimeEvents WHERE StartedAt >= @From ORDER BY StartedAt DESC",
            new { From = from });
    }

    public async Task<IEnumerable<DowntimeEvent>> GetByEquipmentAsync(int equipmentId)
    {
        using var connection = connectionFactory.Create();
        return await connection.QueryAsync<DowntimeEvent>(
            "SELECT * FROM DowntimeEvents WHERE EquipmentId = @EquipmentId ORDER BY StartedAt DESC",
            new { EquipmentId = equipmentId });
    }
}
