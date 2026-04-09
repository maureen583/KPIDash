using Bogus;
using Dapper;
using KPIDash.Data;

namespace KPIDash.Seeder.Generators;

public class EmployeeSeeder(DbConnectionFactory factory)
{
    private static readonly (string Role, int Count)[] RoleDistribution =
    [
        ("General Operator", 5),
        ("Mixers",           3),
        ("Mill Man",         3),
        ("Supervisor",       2),
        ("Maintenance",      2),
    ];

    public List<(int EmployeeId, string Role)> Seed()
    {
        var faker = new Faker("en");
        using var conn = factory.Create();
        var result = new List<(int, string)>();

        foreach (var (role, count) in RoleDistribution)
        {
            for (int i = 0; i < count; i++)
            {
                var firstName = faker.Name.FirstName();
                var lastName = faker.Name.LastName();
                var id = conn.ExecuteScalar<int>("""
                    INSERT INTO Employees (FirstName, LastName, Role)
                    VALUES (@FirstName, @LastName, @Role);
                    SELECT last_insert_rowid();
                    """,
                    new { FirstName = firstName, LastName = lastName, Role = role });

                result.Add((id, role));
            }
        }

        Console.WriteLine($"  Employees: {result.Count} rows");
        return result;
    }
}
