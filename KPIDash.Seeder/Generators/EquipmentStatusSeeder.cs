using Dapper;
using KPIDash.Data;

namespace KPIDash.Seeder.Generators;

public class EquipmentStatusSeeder(DbConnectionFactory factory)
{
    public void Seed(Dictionary<int, List<StateWindow>> timelines)
    {
        using var conn = factory.Create();
        int total = 0;

        foreach (var (equipId, windows) in timelines)
        {
            foreach (var window in windows)
            {
                var dbReason = MapReason(window.Status, window.Reason);
                conn.Execute("""
                    INSERT INTO EquipmentStatus (EquipmentId, RecordedAt, Status, Reason)
                    VALUES (@EquipmentId, @RecordedAt, @Status, @Reason)
                    """,
                    new
                    {
                        EquipmentId = equipId,
                        RecordedAt = window.Start.ToString("o"),
                        Status = window.Status,
                        Reason = dbReason
                    });
                total++;
            }
        }

        Console.WriteLine($"  EquipmentStatus: {total} rows");
    }

    private static string MapReason(string status, string reason) => status switch
    {
        "Running" => "NormalOperation",
        "Idle"    => "Idle",
        "Down" => reason switch
        {
            "TempOutOfRange" => "ParameterOutOfRange",
            "EmergencyStop"  => "EmergencyStop",
            "SafetyGuardOpen"=> "EmergencyStop",
            _                => "FaultTrip"
        },
        _ => reason
    };
}
