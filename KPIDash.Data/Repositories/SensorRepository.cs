using Dapper;
using KPIDash.Data.Models;

namespace KPIDash.Data.Repositories;

public class SensorRepository(DbConnectionFactory connectionFactory) : ISensorRepository
{
    public async Task<IEnumerable<Sensor>> GetByEquipmentIdAsync(int equipmentId)
    {
        using var connection = connectionFactory.Create();
        return await connection.QueryAsync<Sensor>(
            "SELECT * FROM Sensors WHERE EquipmentId = @EquipmentId",
            new { EquipmentId = equipmentId });
    }

    public async Task<IEnumerable<SensorReading>> GetReadingsAsync(int sensorId, DateTime from, DateTime to)
    {
        using var connection = connectionFactory.Create();
        return await connection.QueryAsync<SensorReading>(
            """
            SELECT * FROM SensorReadings
            WHERE SensorId = @SensorId
              AND RecordedAt >= @From
              AND RecordedAt <= @To
            ORDER BY RecordedAt
            """,
            new { SensorId = sensorId, From = from.ToString("o"), To = to.ToString("o") });
    }

    public async Task<SensorReading?> GetLatestReadingAsync(int sensorId)
    {
        using var connection = connectionFactory.Create();
        return await connection.QuerySingleOrDefaultAsync<SensorReading>(
            """
            SELECT * FROM SensorReadings
            WHERE SensorId = @SensorId
            ORDER BY RecordedAt DESC
            LIMIT 1
            """,
            new { SensorId = sensorId });
    }
}
