using Dapper;
using KPIDash.Data.Repositories;
using KPIDash.Tests.Infrastructure;

namespace KPIDash.Tests.Repositories;

public class EquipmentRepositoryTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly EquipmentRepository _sut;

    public EquipmentRepositoryTests()
    {
        _sut = new EquipmentRepository(_db.Factory);
        SeedEquipment();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEquipmentOrderedByDisplayOrder()
    {
        var results = (await _sut.GetAllAsync()).ToList();

        Assert.Equal(4, results.Count);
        Assert.Equal(1, results[0].DisplayOrder);
        Assert.Equal(2, results[1].DisplayOrder);
        Assert.Equal(3, results[2].DisplayOrder);
        Assert.Equal(4, results[3].DisplayOrder);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsEquipment()
    {
        var result = await _sut.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Conveyor", result.Name);
        Assert.Equal("Conveyor", result.Type);
    }

    [Fact]
    public async Task GetByIdAsync_MissingId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(999);

        Assert.Null(result);
    }

    private void SeedEquipment()
    {
        using var conn = _db.Factory.Create();
        conn.Execute("""
            INSERT INTO Equipment (EquipmentId, Name, Type, DisplayOrder) VALUES
            (1, 'Conveyor',       'Conveyor',       1),
            (2, 'Internal Mixer', 'InternalMixer',  2),
            (3, 'Mill',           'Mill',            3),
            (4, 'Cooling Line',   'CoolingLine',     4)
            """);
    }

    public void Dispose() => _db.Dispose();
}
