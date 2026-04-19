using Dapper;
using KPIDash.Data.Repositories;
using KPIDash.Tests.Infrastructure;

namespace KPIDash.Tests.Repositories;

public class EquipmentStatusRepositoryTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly EquipmentStatusRepository _sut;

    private static readonly DateTime Base = new(2025, 1, 15, 8, 0, 0, DateTimeKind.Utc);

    public EquipmentStatusRepositoryTests()
    {
        _sut = new EquipmentStatusRepository(_db.Factory);
        SeedData();
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsNewestStatus()
    {
        var result = await _sut.GetLatestAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Down", result.Status);
        Assert.Equal("FaultTrip", result.Reason);
    }

    [Fact]
    public async Task GetLatestAsync_NoStatusForEquipment_ReturnsNull()
    {
        var result = await _sut.GetLatestAsync(99);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsStatusesWithinRange()
    {
        var from = Base.AddHours(-1);
        var to   = Base.AddHours(1);

        var results = (await _sut.GetHistoryAsync(1, from, to)).ToList();

        Assert.Single(results);
        Assert.Equal("Running", results[0].Status);
    }

    [Fact]
    public async Task GetHistoryAsync_RangeExcludesOutliers()
    {
        var from = Base.AddHours(5);
        var to   = Base.AddHours(24);

        var results = (await _sut.GetHistoryAsync(1, from, to)).ToList();

        Assert.Single(results);
        Assert.Equal("Down", results[0].Status);
    }

    [Fact]
    public async Task GetHistoryAsync_ResultsAreOrderedByRecordedAt()
    {
        var from = Base.AddHours(-1);
        var to   = Base.AddHours(24);

        var results = (await _sut.GetHistoryAsync(1, from, to)).ToList();

        Assert.Equal(2, results.Count);
        Assert.True(results[0].RecordedAt <= results[1].RecordedAt);
    }

    private void SeedData()
    {
        using var conn = _db.Factory.Create();
        conn.Execute("""
            INSERT INTO Equipment (EquipmentId, Name, Type, DisplayOrder) VALUES
            (1, 'Conveyor', 'Conveyor', 1)
            """);

        conn.Execute(
            """
            INSERT INTO EquipmentStatus (EquipmentId, RecordedAt, Status, Reason) VALUES
            (@EquipmentId, @RecordedAt, @Status, @Reason)
            """,
            new[]
            {
                new { EquipmentId = 1, RecordedAt = Base.ToString("o"),             Status = "Running", Reason = "NormalOperation" },
                new { EquipmentId = 1, RecordedAt = Base.AddHours(6).ToString("o"), Status = "Down",    Reason = "FaultTrip"       },
            });
    }

    public void Dispose() => _db.Dispose();
}
