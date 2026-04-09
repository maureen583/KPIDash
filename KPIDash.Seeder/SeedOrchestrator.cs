using Dapper;
using KPIDash.Data;
using KPIDash.Seeder.Generators;

namespace KPIDash.Seeder;

public class SeedOrchestrator(DbConnectionFactory factory)
{
    public void Seed()
    {
        var rng = new Random(42); // fixed seed for reproducibility
        var to = DateTime.UtcNow;
        var from = to.AddDays(-30);

        Console.WriteLine($"Seeding 30 days: {from:yyyy-MM-dd} → {to:yyyy-MM-dd}");
        Console.WriteLine();

        ClearData();
        Console.WriteLine("Cleared existing data.");
        Console.WriteLine();

        // 1. Equipment
        new EquipmentSeeder(factory).Seed();

        // 2. Employees
        var employees = new EmployeeSeeder(factory).Seed();

        // 3. Sensors
        var sensorMap = new SensorSeeder(factory).Seed();

        // 4. Equipment type lookup for timeline generation
        using var conn = factory.Create();
        var equipmentTypes = conn.Query<(int EquipmentId, string Type)>(
            "SELECT EquipmentId, Type FROM Equipment ORDER BY EquipmentId")
            .ToDictionary(e => e.EquipmentId, e => e.Type);

        // 5. Generate timelines for all 8 equipment
        Console.WriteLine("  Generating state timelines...");
        var generator = new TimelineGenerator(rng);
        var timelines = equipmentTypes.ToDictionary(
            kvp => kvp.Key,
            kvp => generator.Generate(kvp.Value, from, to));

        // 6. TimeLog
        var shiftAssignments = new TimeLogSeeder(factory, rng).Seed(employees, from, to);

        // 7. Batches (before SensorReadings; depends on timelines + shifts)
        new BatchSeeder(factory, rng).Seed(timelines, shiftAssignments, to);

        // 8. SensorReadings (largest table — shows progress)
        Console.Write("  SensorReadings: generating...");
        new SensorReadingSeeder(factory, rng).Seed(sensorMap, timelines, from, to);

        // 9. EquipmentStatus (derived from timeline)
        new EquipmentStatusSeeder(factory).Seed(timelines);

        // 10. DowntimeEvents (derived from timeline)
        new DowntimeEventSeeder(factory).Seed(timelines);

        Console.WriteLine();
        Console.WriteLine("Done.");
    }

    private void ClearData()
    {
        using var conn = factory.Create();
        conn.Execute("DELETE FROM DowntimeEvents");
        conn.Execute("DELETE FROM EquipmentStatus");
        conn.Execute("DELETE FROM SensorReadings");
        conn.Execute("DELETE FROM Batches");
        conn.Execute("DELETE FROM TimeLog");
        conn.Execute("DELETE FROM Sensors");
        conn.Execute("DELETE FROM Employees");
        conn.Execute("DELETE FROM Equipment");
        // Reset autoincrement counters
        conn.Execute("DELETE FROM sqlite_sequence WHERE name IN " +
            "('DowntimeEvents','EquipmentStatus','SensorReadings','Batches'," +
            "'TimeLog','Sensors','Employees','Equipment')");
    }
}
