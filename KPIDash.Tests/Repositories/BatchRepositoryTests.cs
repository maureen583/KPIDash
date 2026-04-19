using Dapper;
using KPIDash.Data.Repositories;
using KPIDash.Tests.Infrastructure;

namespace KPIDash.Tests.Repositories;

public class BatchRepositoryTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly BatchRepository _sut;

    // GetTodaysAsync computes today's range from DateTime.UtcNow internally,
    // so seeds that target it must also use UtcNow.
    private static readonly DateTime Today = DateTime.UtcNow.Date;
    private static readonly DateTime Yesterday = Today.AddDays(-1);
    private static readonly DateTime LastWeek = Today.AddDays(-7);

    public BatchRepositoryTests()
    {
        _sut = new BatchRepository(_db.Factory);
        SeedEmployee();
    }

    // --- GetTodaysAsync ---

    [Fact]
    public async Task GetTodaysAsync_ReturnsOnlyTodaysBatches()
    {
        Seed(("B-001", Today.AddHours(6),     "Complete"),
             ("B-002", Yesterday.AddHours(6), "Complete"));

        var results = (await _sut.GetTodaysAsync()).ToList();

        Assert.Single(results);
        Assert.Equal("B-001", results[0].BatchNumber);
    }

    [Fact]
    public async Task GetTodaysAsync_NoBatchesToday_ReturnsEmpty()
    {
        Seed(("B-OLD", Yesterday.AddHours(6), "Complete"));

        var results = await _sut.GetTodaysAsync();

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetTodaysAsync_OrderedByStartedAtDescending()
    {
        Seed(("B-AM",   Today.AddHours(6),  "Complete"),
             ("B-PM",   Today.AddHours(14), "Complete"),
             ("B-NOON", Today.AddHours(12), "Complete"));

        var results = (await _sut.GetTodaysAsync()).ToList();

        Assert.Equal(3, results.Count);
        Assert.Equal("B-PM",   results[0].BatchNumber);
        Assert.Equal("B-NOON", results[1].BatchNumber);
        Assert.Equal("B-AM",   results[2].BatchNumber);
    }

    // --- GetByPeriodAsync ---

    [Fact]
    public async Task GetByPeriodAsync_ReturnsMatchingBatches()
    {
        Seed(("B-A", LastWeek.AddDays(1), "Complete"),
             ("B-B", LastWeek.AddDays(3), "Complete"),
             ("B-C", LastWeek.AddDays(6), "Complete"));

        var from = LastWeek.AddDays(1);
        var to   = LastWeek.AddDays(3);
        var results = (await _sut.GetByPeriodAsync(from, to)).ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.BatchNumber == "B-A");
        Assert.Contains(results, r => r.BatchNumber == "B-B");
    }

    [Fact]
    public async Task GetByPeriodAsync_OrderedByStartedAtDescending()
    {
        Seed(("B-1", LastWeek.AddDays(1), "Complete"),
             ("B-2", LastWeek.AddDays(2), "Complete"),
             ("B-3", LastWeek.AddDays(3), "Complete"));

        var results = (await _sut.GetByPeriodAsync(LastWeek, LastWeek.AddDays(7))).ToList();

        Assert.True(results[0].StartedAt >= results[1].StartedAt);
        Assert.True(results[1].StartedAt >= results[2].StartedAt);
    }

    [Fact]
    public async Task GetByPeriodAsync_NoMatchingBatches_ReturnsEmpty()
    {
        Seed(("B-OLD", LastWeek.AddDays(1), "Complete"));

        var results = await _sut.GetByPeriodAsync(Today, Today.AddDays(1));

        Assert.Empty(results);
    }

    // --- GetLastAsync ---

    [Fact]
    public async Task GetLastAsync_ReturnsMostRecentBatch()
    {
        Seed(("B-EARLY", LastWeek,            "Complete"),
             ("B-LATE",  Yesterday.AddHours(8), "Complete"));

        var result = await _sut.GetLastAsync();

        Assert.NotNull(result);
        Assert.Equal("B-LATE", result.BatchNumber);
    }

    [Fact]
    public async Task GetLastAsync_NoBatches_ReturnsNull()
    {
        var result = await _sut.GetLastAsync();

        Assert.Null(result);
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsBatch()
    {
        Seed(("B-X", Today.AddHours(6), "InProgress"));
        using var conn = _db.Factory.Create();
        var id = conn.ExecuteScalar<int>("SELECT BatchId FROM Batches WHERE BatchNumber = 'B-X'");

        var result = await _sut.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal("B-X", result.BatchNumber);
        Assert.Equal("InProgress", result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_MissingId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(999);

        Assert.Null(result);
    }

    private void SeedEmployee()
    {
        using var conn = _db.Factory.Create();
        conn.Execute("""
            INSERT INTO Employees (EmployeeId, FirstName, LastName, Role)
            VALUES (1, 'Test', 'Operator', 'Operator')
            """);
    }

    private void Seed(params (string BatchNumber, DateTime StartedAt, string Status)[] batches)
    {
        using var conn = _db.Factory.Create();
        foreach (var (number, startedAt, status) in batches)
        {
            conn.Execute("""
                INSERT INTO Batches (BatchNumber, StartedAt, TargetDumpTemp, Status, OperatorId)
                VALUES (@BatchNumber, @StartedAt, 150.0, @Status, 1)
                """,
                new { BatchNumber = number, StartedAt = startedAt.ToString("o"), Status = status });
        }
    }

    public void Dispose() => _db.Dispose();
}
