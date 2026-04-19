using Dapper;
using KPIDash.Data.Repositories;
using KPIDash.Tests.Infrastructure;

namespace KPIDash.Tests.Repositories;

public class TimeLogRepositoryTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly TimeLogRepository _sut;

    private static readonly DateTime Base = new(2025, 6, 10, 6, 0, 0, DateTimeKind.Utc);

    public TimeLogRepositoryTests()
    {
        _sut = new TimeLogRepository(_db.Factory);
        SeedEmployee();
    }

    // --- GetByShiftDateAsync ---

    [Fact]
    public async Task GetByShiftDateAsync_ReturnsOnlyMatchingShiftDate()
    {
        Seed(1, Base,             Base.AddHours(8),  "2025-06-10", "Day");
        Seed(1, Base.AddDays(1),  null,              "2025-06-11", "Day");

        var results = (await _sut.GetByShiftDateAsync("2025-06-10")).ToList();

        Assert.Single(results);
        Assert.Equal("2025-06-10", results[0].ShiftDate);
    }

    [Fact]
    public async Task GetByShiftDateAsync_OrderedByClockInAscending()
    {
        Seed(1, Base.AddHours(6), null, "2025-06-10", "Day");
        Seed(1, Base.AddHours(2), null, "2025-06-10", "Day");
        Seed(1, Base.AddHours(4), null, "2025-06-10", "Day");

        var results = (await _sut.GetByShiftDateAsync("2025-06-10")).ToList();

        Assert.Equal(3, results.Count);
        Assert.True(results[0].ClockIn <= results[1].ClockIn);
        Assert.True(results[1].ClockIn <= results[2].ClockIn);
    }

    [Fact]
    public async Task GetByShiftDateAsync_NoLogsForDate_ReturnsEmpty()
    {
        var results = await _sut.GetByShiftDateAsync("2099-01-01");

        Assert.Empty(results);
    }

    // --- GetActiveAsync ---

    [Fact]
    public async Task GetActiveAsync_ReturnsOnlyLogsWithNullClockOut()
    {
        Seed(1, Base,            Base.AddHours(8), "2025-06-10", "Day");
        Seed(1, Base.AddHours(8), null,            "2025-06-10", "Afternoon");

        var results = (await _sut.GetActiveAsync()).ToList();

        Assert.Single(results);
        Assert.Null(results[0].ClockOut);
        Assert.Equal("Afternoon", results[0].Shift);
    }

    [Fact]
    public async Task GetActiveAsync_NoActiveEmployees_ReturnsEmpty()
    {
        Seed(1, Base, Base.AddHours(8), "2025-06-10", "Day");

        var results = await _sut.GetActiveAsync();

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetActiveAsync_OrderedByClockInAscending()
    {
        Seed(1, Base.AddHours(6), null, "2025-06-10", "Day");
        Seed(1, Base.AddHours(2), null, "2025-06-10", "Day");
        Seed(1, Base.AddHours(4), null, "2025-06-10", "Day");

        var results = (await _sut.GetActiveAsync()).ToList();

        Assert.Equal(3, results.Count);
        Assert.True(results[0].ClockIn <= results[1].ClockIn);
        Assert.True(results[1].ClockIn <= results[2].ClockIn);
    }

    private void SeedEmployee()
    {
        using var conn = _db.Factory.Create();
        conn.Execute("""
            INSERT INTO Employees (EmployeeId, FirstName, LastName, Role)
            VALUES (1, 'Test', 'User', 'Operator')
            """);
    }

    private void Seed(int employeeId, DateTime clockIn, DateTime? clockOut, string shiftDate, string shift)
    {
        using var conn = _db.Factory.Create();
        conn.Execute("""
            INSERT INTO TimeLog (EmployeeId, ClockIn, ClockOut, ShiftDate, Shift)
            VALUES (@EmployeeId, @ClockIn, @ClockOut, @ShiftDate, @Shift)
            """,
            new
            {
                EmployeeId = employeeId,
                ClockIn    = clockIn.ToString("o"),
                ClockOut   = clockOut?.ToString("o"),
                ShiftDate  = shiftDate,
                Shift      = shift
            });
    }

    public void Dispose() => _db.Dispose();
}
