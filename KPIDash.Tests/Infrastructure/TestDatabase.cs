using System.Data;
using Dapper;
using KPIDash.Data;
using Microsoft.Data.Sqlite;

namespace KPIDash.Tests.Infrastructure;

/// <summary>
/// Shared in-memory SQLite database for a test class. Keeps one anchor connection
/// open so the named :memory: database survives across multiple Create() calls.
/// Runs all migration scripts in order, matching DatabaseInitializer behaviour.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _anchor;

    public DbConnectionFactory Factory { get; }

    public TestDatabase()
    {
        var name = Guid.NewGuid().ToString("N");
        var cs = $"Data Source=file:{name}?mode=memory&cache=shared";

        _anchor = new SqliteConnection(cs);
        _anchor.Open();

        var scriptsPath = Path.Combine(AppContext.BaseDirectory, "Scripts");
        foreach (var file in Directory.GetFiles(scriptsPath, "*.sql").OrderBy(f => f))
            _anchor.Execute(File.ReadAllText(file));

        Factory = new TestDbConnectionFactory(cs);
    }

    public void Dispose() => _anchor.Dispose();

    private sealed class TestDbConnectionFactory(string connectionString) : DbConnectionFactory(connectionString) { }
}
