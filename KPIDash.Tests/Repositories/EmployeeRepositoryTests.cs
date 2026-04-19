using Dapper;
using KPIDash.Data.Repositories;
using KPIDash.Tests.Infrastructure;

namespace KPIDash.Tests.Repositories;

public class EmployeeRepositoryTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly EmployeeRepository _sut;

    public EmployeeRepositoryTests()
    {
        _sut = new EmployeeRepository(_db.Factory);
    }

    // --- GetAllAsync ---

    [Fact]
    public async Task GetAllAsync_ReturnsAllEmployees()
    {
        Seed((1, "Alice", "Zane",   "Operator"),
             (2, "Bob",   "Anders", "Supervisor"));

        var results = (await _sut.GetAllAsync()).ToList();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task GetAllAsync_OrderedByLastNameThenFirstName()
    {
        Seed((1, "Charlie", "Smith",  "Operator"),
             (2, "Alice",   "Smith",  "Operator"),
             (3, "Bob",     "Anders", "Supervisor"));

        var results = (await _sut.GetAllAsync()).ToList();

        Assert.Equal("Anders", results[0].LastName);
        Assert.Equal("Alice",  results[1].FirstName);
        Assert.Equal("Charlie", results[2].FirstName);
    }

    [Fact]
    public async Task GetAllAsync_NoEmployees_ReturnsEmpty()
    {
        var results = await _sut.GetAllAsync();

        Assert.Empty(results);
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsEmployee()
    {
        Seed((1, "Dana", "Lee", "Maintenance"));

        var result = await _sut.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Dana", result.FirstName);
        Assert.Equal("Lee",  result.LastName);
        Assert.Equal("Maintenance", result.Role);
    }

    [Fact]
    public async Task GetByIdAsync_MissingId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(999);

        Assert.Null(result);
    }

    private void Seed(params (int Id, string First, string Last, string Role)[] employees)
    {
        using var conn = _db.Factory.Create();
        foreach (var (id, first, last, role) in employees)
        {
            conn.Execute("""
                INSERT INTO Employees (EmployeeId, FirstName, LastName, Role)
                VALUES (@Id, @First, @Last, @Role)
                """,
                new { Id = id, First = first, Last = last, Role = role });
        }
    }

    public void Dispose() => _db.Dispose();
}
