using Dapper;
using KPIDash.Data.Repositories;
using KPIDash.Tests.Infrastructure;

namespace KPIDash.Tests.Repositories;

public class ProductionScheduleRepositoryTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly ProductionScheduleRepository _sut;

    private static readonly DateTime Base = new(2026, 4, 19, 6, 0, 0, DateTimeKind.Utc);

    public ProductionScheduleRepositoryTests()
    {
        _sut = new ProductionScheduleRepository(_db.Factory);
    }

    [Fact]
    public async Task GetByShiftAsync_ReturnsOnlyMatchingShiftAndDate()
    {
        Seed("2026-04-19", "Day",       "Line 1", "NR-100", Base,             Base.AddHours(4));
        Seed("2026-04-19", "Day",       "Line 2", "SBR-200", Base,            Base.AddHours(4));
        Seed("2026-04-19", "Afternoon", "Line 1", "EPDM-300", Base.AddHours(8), Base.AddHours(16));
        Seed("2026-04-18", "Day",       "Line 1", "CR-500",  Base.AddDays(-1),  Base.AddDays(-1).AddHours(4));

        var results = (await _sut.GetByShiftAsync("2026-04-19", "Day")).ToList();

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal("2026-04-19", r.ShiftDate));
        Assert.All(results, r => Assert.Equal("Day", r.Shift));
    }

    [Fact]
    public async Task GetByShiftAsync_OrderedByLineThenScheduledStart()
    {
        Seed("2026-04-19", "Day", "Line 2", "SBR-200", Base.AddHours(4), Base.AddHours(8));
        Seed("2026-04-19", "Day", "Line 1", "NR-100",  Base,             Base.AddHours(4));
        Seed("2026-04-19", "Day", "Line 1", "CR-500",  Base.AddHours(4), Base.AddHours(8));

        var results = (await _sut.GetByShiftAsync("2026-04-19", "Day")).ToList();

        Assert.Equal(3, results.Count);
        Assert.Equal("Line 1", results[0].Line);
        Assert.Equal("NR-100",  results[0].CompoundCode);
        Assert.Equal("Line 1", results[1].Line);
        Assert.Equal("CR-500",  results[1].CompoundCode);
        Assert.Equal("Line 2", results[2].Line);
    }

    [Fact]
    public async Task GetByShiftAsync_ReturnsScheduleProperties()
    {
        Seed("2026-04-19", "Day", "Line 1", "NR-100", Base, Base.AddHours(3), targetBatches: 18, plannedOps: 5);

        var results = (await _sut.GetByShiftAsync("2026-04-19", "Day")).ToList();

        Assert.Single(results);
        var run = results[0];
        Assert.Equal("NR-100",            run.CompoundCode);
        Assert.Equal("Natural Rubber Base", run.CompoundName);
        Assert.Equal(18,                   run.TargetBatches);
        Assert.Equal(5,                    run.PlannedOperators);
    }

    [Fact]
    public async Task GetByShiftAsync_NoMatchingShift_ReturnsEmpty()
    {
        var results = await _sut.GetByShiftAsync("2026-04-19", "Night");

        Assert.Empty(results);
    }

    private void Seed(
        string shiftDate, string shift, string line, string compoundCode,
        DateTime start, DateTime end,
        int targetBatches = 24, int plannedOps = 4)
    {
        using var conn = _db.Factory.Create();
        conn.Execute("""
            INSERT INTO ProductionSchedule
                (ShiftDate, Shift, Line, CompoundCode, CompoundName,
                 ScheduledStart, ScheduledEnd, TargetBatches, PlannedOperators)
            VALUES
                (@ShiftDate, @Shift, @Line, @CompoundCode, @CompoundName,
                 @ScheduledStart, @ScheduledEnd, @TargetBatches, @PlannedOperators)
            """,
            new
            {
                ShiftDate        = shiftDate,
                Shift            = shift,
                Line             = line,
                CompoundCode     = compoundCode,
                CompoundName     = "Natural Rubber Base",
                ScheduledStart   = start.ToString("o"),
                ScheduledEnd     = end.ToString("o"),
                TargetBatches    = targetBatches,
                PlannedOperators = plannedOps,
            });
    }

    public void Dispose() => _db.Dispose();
}
