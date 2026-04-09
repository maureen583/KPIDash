using System.Data;
using Dapper;

namespace KPIDash.Data;

public class DatabaseInitializer(DbConnectionFactory connectionFactory)
{
    private readonly string _scriptsPath = Path.Combine(
        AppContext.BaseDirectory, "Scripts");

    public void Initialize()
    {
        using var connection = connectionFactory.Create();
        connection.Open();

        EnsureMigrationsTable(connection);

        var scripts = Directory.GetFiles(_scriptsPath, "*.sql")
            .OrderBy(f => f);

        foreach (var scriptPath in scripts)
        {
            var fileName = Path.GetFileName(scriptPath);

            var alreadyApplied = connection.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM Migrations WHERE FileName = @FileName",
                new { FileName = fileName }) > 0;

            if (alreadyApplied) continue;

            var sql = File.ReadAllText(scriptPath);
            connection.Execute(sql);
            connection.Execute(
                "INSERT INTO Migrations (FileName, AppliedAt) VALUES (@FileName, @AppliedAt)",
                new { FileName = fileName, AppliedAt = DateTime.UtcNow.ToString("o") });
        }
    }

    private static void EnsureMigrationsTable(IDbConnection connection)
    {
        connection.Execute("""
            CREATE TABLE IF NOT EXISTS Migrations (
                MigrationId INTEGER PRIMARY KEY AUTOINCREMENT,
                FileName    TEXT NOT NULL UNIQUE,
                AppliedAt   TEXT NOT NULL
            )
            """);
    }
}
