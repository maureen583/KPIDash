using KPIDash.Data.Models;

namespace KPIDash.Data.Repositories;

public interface IEquipmentRepository
{
    Task<IEnumerable<Equipment>> GetAllAsync();
    Task<Equipment?> GetByIdAsync(int equipmentId);
}
