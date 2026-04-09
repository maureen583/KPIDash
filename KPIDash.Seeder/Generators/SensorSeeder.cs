using Dapper;
using KPIDash.Data;

namespace KPIDash.Seeder.Generators;

public class SensorSeeder(DbConnectionFactory factory)
{
    // (Name, Unit, MinNormal, MaxNormal, IsStatusSensor)
    private static readonly (string Name, string Unit, double Min, double Max, bool IsStatus)[] ConveyorSensors =
    [
        ("BeltSpeed",        "m/min", 0.5,  3.0,   true),
        ("MotorCurrent",     "A",     5.0,  45.0,  false),
        ("MaterialPresence", "bool",  0.0,  1.0,   false),
    ];

    private static readonly (string Name, string Unit, double Min, double Max, bool IsStatus)[] MixerSensors =
    [
        ("RotorRPM",          "RPM", 20.0,  80.0,  true),
        ("ChamberTemp",       "C",   60.0,  160.0, false),
        ("MotorCurrent",      "A",   50.0,  400.0, false),
        ("RamPressure",       "bar",  2.0,   8.0,  false),
        ("HydraulicPressure", "bar", 80.0,  160.0, false),
    ];

    private static readonly (string Name, string Unit, double Min, double Max, bool IsStatus)[] MillSensors =
    [
        ("FrontRollRPM",  "RPM", 15.0,  40.0,  true),
        ("BackRollRPM",   "RPM", 15.0,  40.0,  false),
        ("FrontRollTemp", "C",   50.0,  90.0,  false),
        ("BackRollTemp",  "C",   50.0,  90.0,  false),
        ("MotorCurrent",  "A",   20.0,  150.0, false),
    ];

    private static readonly (string Name, string Unit, double Min, double Max, bool IsStatus)[] CoolingSensors =
    [
        ("ConveyorSpeed",     "m/min",  0.5,  2.0,  true),
        ("WaterFlowRate",     "L/min", 10.0, 50.0,  false),
        ("WaterInletTemp",    "C",     10.0, 25.0,  false),
        ("WaterOutletTemp",   "C",     20.0, 45.0,  false),
        ("ExitCompoundTemp",  "C",     40.0, 80.0,  false),
    ];

    public Dictionary<int, List<(int SensorId, string Name, string Unit, double Min, double Max, bool IsStatus)>>
        Seed()
    {
        using var conn = factory.Create();

        var equipment = conn.Query<(int EquipmentId, string Type)>(
            "SELECT EquipmentId, Type FROM Equipment ORDER BY EquipmentId").ToList();

        var result = new Dictionary<int, List<(int, string, string, double, double, bool)>>();
        int totalSensors = 0;

        foreach (var (equipId, type) in equipment)
        {
            var specs = type switch
            {
                "Conveyor"       => ConveyorSensors,
                "InternalMixer"  => MixerSensors,
                "Mill"           => MillSensors,
                "CoolingLine"    => CoolingSensors,
                _ => throw new InvalidOperationException($"Unknown equipment type: {type}")
            };

            result[equipId] = [];

            foreach (var (name, unit, min, max, isStatus) in specs)
            {
                var sensorId = conn.ExecuteScalar<int>("""
                    INSERT INTO Sensors (EquipmentId, Name, Unit, MinNormal, MaxNormal, IsStatusSensor)
                    VALUES (@EquipmentId, @Name, @Unit, @Min, @Max, @IsStatus);
                    SELECT last_insert_rowid();
                    """,
                    new { EquipmentId = equipId, Name = name, Unit = unit, Min = min, Max = max,
                          IsStatus = isStatus ? 1 : 0 });

                result[equipId].Add((sensorId, name, unit, min, max, isStatus));
                totalSensors++;
            }
        }

        Console.WriteLine($"  Sensors: {totalSensors} rows");
        return result;
    }
}
