using System.Data;
using Microsoft.Data.Sqlite;

namespace KPIDash.Data;

public class DbConnectionFactory(string connectionString)
{
    public IDbConnection Create() => new SqliteConnection(connectionString);
}
