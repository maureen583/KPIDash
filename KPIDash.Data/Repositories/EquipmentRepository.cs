using Dapper;
using KPIDash.Data.Models;

namespace KPIDash.Data.Repositories;

public class EquipmentRepository(DbConnectionFactory connectionFactory) : IEquipmentRepository
{
    public async Task<IEnumerable<Equipment>> GetAllAsync()
    {
        using var connection = connectionFactory.Create();
        return await connection.QueryAsync<Equipment>(
            "SELECT * FROM Equipment ORDER BY DisplayOrder");
    }

    public async Task<Equipment?> GetByIdAsync(int equipmentId)
    {
        using var connection = connectionFactory.Create();
        return await connection.QuerySingleOrDefaultAsync<Equipment>(
            "SELECT * FROM Equipment WHERE EquipmentId = @EquipmentId",
            new { EquipmentId = equipmentId });
    }
}
