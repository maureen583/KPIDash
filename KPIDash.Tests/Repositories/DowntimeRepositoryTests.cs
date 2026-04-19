using Dapper;
using KPIDash.Data.Repositories;
using KPIDash.Tests.Infrastructure;

namespace KPIDash.Tests.Repositories;

public class DowntimeRepositoryTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly DowntimeRepository _sut;

    // GetRecentAsync computes its cutoff from DateTime.UtcNow internally,
    // so seeds targeting it must be relative to UtcNow as well.
    private static readonly DateTime Now = DateTime.UtcNow;

    public DowntimeRepositoryTests()
    {
        _sut = new DowntimeRepository(_db.Factory);
        SeedEquipment();
    }

    // --- GetRecentAsync ---

    [Fact]
    public async Task GetRecentAsync_DefaultDays_ExcludesOlderEvents()
    {
        SeedDowntime(1, Now.AddDays(-3), "FaultTrip");
        SeedDowntime(1, Now.AddDays(-10), "EmergencyStop");

        var results = (await _sut.GetRecentAsync()).ToList();

        Assert.Single(results);
        Assert.Equal("FaultTrip", results[0].Reason);
    }

    [Fact]
    public async Task GetRecentAsync_CustomDays_UsesCorrectCutoff()
    {
        SeedDowntime(1, Now.AddDays(-1),  "FaultTrip");
        SeedDowntime(1, Now.AddDays(-4),  "ParameterOutOfRange");
        SeedDowntime(1, Now.AddDays(-10), "EmergencyStop");

        var results = (await _sut.GetRecentAsync(days: 5)).ToList();

        Assert.Equal(2, results.Count);
        Assert.DoesNotContain(results, r => r.Reason == "EmergencyStop");
    }

    [Fact]
    public async Task GetRecentAsync_OrderedByStartedAtDescending()
    {
        SeedDowntime(1, Now.AddDays(-1), "FaultTrip");
        SeedDowntime(1, Now.AddDays(-3), "ParameterOutOfRange");
        SeedDowntime(1, Now.AddDays(-5), "PlannedIdle");

        var results = (await _sut.GetRecentAsync(days: 7)).ToList();

        Assert.True(results[0].StartedAt >= results[1].StartedAt);
        Assert.True(results[1].StartedAt >= results[2].StartedAt);
    }

    [Fact]
    public async Task GetRecentAsync_NoRecentEvents_ReturnsEmpty()
    {
        SeedDowntime(1, Now.AddDays(-30), "FaultTrip");

        var results = await _sut.GetRecentAsync();

        Assert.Empty(results);
    }

    // --- GetByEquipmentAsync ---

    [Fact]
    public async Task GetByEquipmentAsync_ReturnsOnlyMatchingEquipment()
    {
        SeedDowntime(1, Now.AddDays(-1), "FaultTrip");
        SeedDowntime(2, Now.AddDays(-2), "EmergencyStop");

        var results = (await _sut.GetByEquipmentAsync(1)).ToList();

        Assert.Single(results);
        Assert.Equal(1, results[0].EquipmentId);
    }

    [Fact]
    public async Task GetByEquipmentAsync_OrderedByStartedAtDescending()
    {
        SeedDowntime(1, Now.AddDays(-1), "FaultTrip");
        SeedDowntime(1, Now.AddDays(-3), "ParameterOutOfRange");
        SeedDowntime(1, Now.AddDays(-5), "PlannedIdle");

        var results = (await _sut.GetByEquipmentAsync(1)).ToList();

        Assert.Equal(3, results.Count);
        Assert.True(results[0].StartedAt >= results[1].StartedAt);
        Assert.True(results[1].StartedAt >= results[2].StartedAt);
    }

    [Fact]
    public async Task GetByEquipmentAsync_NoEventsForEquipment_ReturnsEmpty()
    {
        var results = await _sut.GetByEquipmentAsync(99);

        Assert.Empty(results);
    }

    private void SeedEquipment()
    {
        using var conn = _db.Factory.Create();
        conn.Execute("""
            INSERT INTO Equipment (EquipmentId, Name, Type, DisplayOrder) VALUES
            (1, 'Conveyor',       'Conveyor',      1),
            (2, 'Internal Mixer', 'InternalMixer', 2)
            """);
    }

    private void SeedDowntime(int equipmentId, DateTime startedAt, string reason)
    {
        using var conn = _db.Factory.Create();
        conn.Execute("""
            INSERT INTO DowntimeEvents (EquipmentId, StartedAt, Reason)
            VALUES (@EquipmentId, @StartedAt, @Reason)
            """,
            new { EquipmentId = equipmentId, StartedAt = startedAt.ToString("o"), Reason = reason });
    }

    public void Dispose() => _db.Dispose();
}
