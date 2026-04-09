using KPIDash.Data.Models;

namespace KPIDash.Data.Repositories;

public interface ISensorRepository
{
    Task<IEnumerable<Sensor>> GetByEquipmentIdAsync(int equipmentId);
    Task<IEnumerable<SensorReading>> GetReadingsAsync(int sensorId, DateTime from, DateTime to);
    Task<SensorReading?> GetLatestReadingAsync(int sensorId);
}
