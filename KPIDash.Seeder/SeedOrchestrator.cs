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
        var from = to.AddDays(-7);

        Console.WriteLine($"Seeding 7 days: {from:yyyy-MM-dd} → {to:yyyy-MM-dd}");
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

        // 5. Generate timelines per line, cascading downstream states from upstream
        Console.WriteLine("  Generating state timelines...");
        var generator = new TimelineGenerator(rng);
        var timelines = new Dictionary<int, List<StateWindow>>();

        // Line 1: 1→3→5→7  |  Line 2: 2→4→6→8
        int[][] linePipelines = [[1, 3, 5, 7], [2, 4, 6, 8]];
        foreach (var pipeline in linePipelines)
        {
            List<StateWindow>? upstreamTimeline = null;
            foreach (var equipId in pipeline)
            {
                var type = equipmentTypes[equipId];
                var timeline = generator.Generate(type, from, to);
                if (upstreamTimeline != null)
                    timeline = generator.ApplyCascade(timeline, upstreamTimeline);
                timelines[equipId] = timeline;
                upstreamTimeline = timeline;
            }
        }

        // 6. TimeLog
        var shiftAssignments = new TimeLogSeeder(factory, rng).Seed(employees, from, to);

        // 7. ProductionSchedule (before Batches — Batches reference compound codes)
        var scheduleRuns = new ProductionScheduleSeeder(factory, rng).Seed(from, to);

        // 8. Batches (before SensorReadings; depends on timelines + shifts + schedule)
        new BatchSeeder(factory, rng).Seed(timelines, shiftAssignments, scheduleRuns, to);

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
        conn.Execute("DELETE FROM ProductionSchedule");
        conn.Execute("DELETE FROM TimeLog");
        conn.Execute("DELETE FROM Sensors");
        conn.Execute("DELETE FROM Employees");
        conn.Execute("DELETE FROM Equipment");
        // Reset autoincrement counters
        conn.Execute("DELETE FROM sqlite_sequence WHERE name IN " +
            "('DowntimeEvents','EquipmentStatus','SensorReadings','Batches','ProductionSchedule'," +
            "'TimeLog','Sensors','Employees','Equipment')");
    }
}
