using Dapper;

namespace KPIDash.Data;

public class DataSeeder(DbConnectionFactory connectionFactory)
{
    public void Seed()
    {
        using var connection = connectionFactory.Create();

        var count = connection.ExecuteScalar<int>("SELECT COUNT(1) FROM Equipment");
        if (count > 0) return;

        connection.Execute("""
            INSERT INTO Equipment (Name, Type, DisplayOrder) VALUES
            ('Line 1 Conveyor',    'Conveyor',       1),
            ('Line 2 Conveyor',    'Conveyor',       2),
            ('Line 1 Internal Mixer',   'InternalMixer',  3),
            ('Line 2 Internal Mixer',   'InternalMixer',  4),
            ('Line 1 Mill',             'Mill',           5),
            ('Line 2 Mill',             'Mill',           6),
            ('Line 1 Cooling',     'CoolingLine',    7),
            ('Line 2 Cooling',     'CoolingLine',    8)
            """);
    }
}
