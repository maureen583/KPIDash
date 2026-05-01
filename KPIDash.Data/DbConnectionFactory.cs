using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace KPIDash.Data;

public class DbConnectionFactory(string connectionString)
{
    static DbConnectionFactory() => SqlMapper.AddTypeHandler(new UtcDateTimeHandler());

    public virtual IDbConnection Create() => new SqliteConnection(connectionString);
}
