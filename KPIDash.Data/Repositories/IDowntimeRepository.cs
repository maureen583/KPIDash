using KPIDash.Data.Models;

namespace KPIDash.Data.Repositories;

public interface IDowntimeRepository
{
    Task<IEnumerable<DowntimeEvent>> GetRecentAsync(int days = 7);
    Task<IEnumerable<DowntimeEvent>> GetByEquipmentAsync(int equipmentId);
}
