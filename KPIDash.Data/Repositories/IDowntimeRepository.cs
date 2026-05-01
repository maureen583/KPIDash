using KPIDash.Data.Models;

namespace KPIDash.Data.Repositories;

public interface IDowntimeRepository
{
    Task<IEnumerable<DowntimeEvent>> GetRecentAsync(int days = 7);
    Task<IEnumerable<DowntimeEvent>> GetByEquipmentAsync(int equipmentId);
    Task<IEnumerable<DowntimeEvent>> GetByShiftAsync(DateTime from, DateTime to);
    Task UpdateCategoryAsync(int downtimeId, string category);
}
