using System.Data;
using KPIDash.Data;
using Microsoft.Data.Sqlite;

namespace KPIDash.Seeder;

public class SeederConnectionFactory(string connectionString) : DbConnectionFactory(connectionString)
{
    public override IDbConnection Create()
    {
        var conn = (SqliteConnection)base.Create();
        conn.StateChange += (_, e) =>
        {
            if (e.CurrentState != ConnectionState.Open) return;
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                PRAGMA journal_mode = WAL;
                PRAGMA synchronous = OFF;
                PRAGMA temp_store = MEMORY;
                PRAGMA cache_size = -65536;
                """;
            cmd.ExecuteNonQuery();
        };
        return conn;
    }
}
