using KPIDash.Data.Models;

namespace KPIDash.Data.Repositories;

public interface IEquipmentStatusRepository
{
    Task<EquipmentStatus?> GetLatestAsync(int equipmentId);
    Task<IEnumerable<EquipmentStatus>> GetHistoryAsync(int equipmentId, DateTime from, DateTime to);
}
