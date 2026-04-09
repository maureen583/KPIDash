using Dapper;
using KPIDash.Data.Models;

namespace KPIDash.Data.Repositories;

public class EquipmentStatusRepository(DbConnectionFactory connectionFactory) : IEquipmentStatusRepository
{
    public async Task<EquipmentStatus?> GetLatestAsync(int equipmentId)
    {
        using var connection = connectionFactory.Create();
        return await connection.QuerySingleOrDefaultAsync<EquipmentStatus>(
            """
            SELECT * FROM EquipmentStatus
            WHERE EquipmentId = @EquipmentId
            ORDER BY RecordedAt DESC
            LIMIT 1
            """,
            new { EquipmentId = equipmentId });
    }

    public async Task<IEnumerable<EquipmentStatus>> GetHistoryAsync(int equipmentId, DateTime from, DateTime to)
    {
        using var connection = connectionFactory.Create();
        return await connection.QueryAsync<EquipmentStatus>(
            """
            SELECT * FROM EquipmentStatus
            WHERE EquipmentId = @EquipmentId
              AND RecordedAt >= @From
              AND RecordedAt <= @To
            ORDER BY RecordedAt
            """,
            new { EquipmentId = equipmentId, From = from.ToString("o"), To = to.ToString("o") });
    }
}
