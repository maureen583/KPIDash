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

    public async Task<IEnumerable<DowntimeEvent>> GetByShiftAsync(DateTime from, DateTime to)
    {
        using var connection = connectionFactory.Create();
        return await connection.QueryAsync<DowntimeEvent>(
            """
            SELECT d.*, e.Name AS EquipmentName
            FROM DowntimeEvents d
            JOIN Equipment e ON e.EquipmentId = d.EquipmentId
            WHERE d.StartedAt >= @From AND d.StartedAt < @To
            ORDER BY d.StartedAt
            """,
            new { From = DateTime.SpecifyKind(from, DateTimeKind.Utc).ToString("o"),
                  To   = DateTime.SpecifyKind(to,   DateTimeKind.Utc).ToString("o") });
    }

    public async Task UpdateCategoryAsync(int downtimeId, string category)
    {
        using var connection = connectionFactory.Create();
        await connection.ExecuteAsync(
            "UPDATE DowntimeEvents SET Category = @Category WHERE DowntimeId = @DowntimeId",
            new { DowntimeId = downtimeId, Category = category });
    }
}
