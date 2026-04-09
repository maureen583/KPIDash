using Dapper;
using KPIDash.Data;

namespace KPIDash.Seeder.Generators;

public class SensorReadingSeeder(DbConnectionFactory factory, Random rng)
{
    private const int BatchSize = 1000;
    private const int IntervalMinutes = 5;

    // Sensors where Idle means near-zero (motion/current sensors)
    private static readonly HashSet<string> MotionSensors =
        ["BeltSpeed", "RotorRPM", "FrontRollRPM", "BackRollRPM", "ConveyorSpeed"];

    private static readonly HashSet<string> CurrentSensors =
        ["MotorCurrent"];

    private static readonly HashSet<string> PressureSensors =
        ["RamPressure", "HydraulicPressure", "WaterFlowRate"];

    public void Seed(
        Dictionary<int, List<(int SensorId, string Name, string Unit, double Min, double Max, bool IsStatus)>> sensorMap,
        Dictionary<int, List<StateWindow>> timelines,
        DateTime from, DateTime to)
    {
        using var conn = factory.Create();
        conn.Open();

        int total = 0;
        var buffer = new List<object>(BatchSize);

        void Flush()
        {
            using var tx = conn.BeginTransaction();
            conn.Execute(
                "INSERT INTO SensorReadings (SensorId, RecordedAt, Value) VALUES (@SensorId, @RecordedAt, @Value)",
                buffer, tx);
            tx.Commit();
            total += buffer.Count;
            buffer.Clear();
        }

        foreach (var (equipId, sensors) in sensorMap)
        {
            if (!timelines.TryGetValue(equipId, out var timeline)) continue;

            var slots = EnumerateSlots(from, to).ToList();

            foreach (var (sensorId, name, unit, min, max, _) in sensors)
            {
                int slotIndex = 0;
                int windowIndex = 0;

                foreach (var slot in slots)
                {
                    // Advance window index to the window containing this slot
                    while (windowIndex < timeline.Count - 1 && timeline[windowIndex].End <= slot)
                        windowIndex++;

                    var window = timeline[windowIndex];
                    var value = GenerateValue(name, window.Status, window.Reason, min, max, slotIndex);

                    buffer.Add(new { SensorId = sensorId, RecordedAt = slot.ToString("o"), Value = value });

                    if (buffer.Count >= BatchSize)
                        Flush();

                    slotIndex++;
                }
            }
        }

        if (buffer.Count > 0) Flush();
        Console.WriteLine($"  SensorReadings: {total} rows");
    }

    private double GenerateValue(string sensorName, string status, string downReason,
        double min, double max, int slotIndex)
    {
        var midpoint = (min + max) / 2.0;
        var range = max - min;

        return status switch
        {
            "Running" => Clamp(NextGaussian(midpoint, range * 0.03), min, max),
            "Idle"    => GenerateIdleValue(sensorName, min, max, midpoint),
            "Down"    => GenerateDownValue(sensorName, downReason, min, max, midpoint, range, slotIndex),
            _         => midpoint
        };
    }

    private double GenerateIdleValue(string sensorName, double min, double max, double midpoint)
    {
        if (MotionSensors.Contains(sensorName))
            return 0.0;

        if (CurrentSensors.Contains(sensorName))
            return NextGaussian(1.0, 0.3); // residual 0-2A

        if (PressureSensors.Contains(sensorName))
            return Clamp(NextGaussian(min, (max - min) * 0.05), min * 0.8, min * 1.2);

        // Temperature sensors drift toward ambient (20°C)
        return Clamp(NextGaussian(midpoint * 0.6, (max - min) * 0.05), min, max);
    }

    private double GenerateDownValue(string sensorName, string reason, double min, double max,
        double midpoint, double range, int slotIndex)
    {
        var degradation = Math.Min(slotIndex / 3.0, 1.0); // ramp over ~3 readings

        // Check if this sensor is the trigger for the given failure reason
        bool isTrigger = IsFailureTrigger(sensorName, reason);

        if (isTrigger)
        {
            // Drift beyond normal range
            bool goesHigh = GoesHigh(sensorName, reason);
            return goesHigh
                ? max + range * (0.15 + degradation * 0.15)
                : min - range * (0.15 + degradation * 0.15);
        }

        // Secondary sensors: degrade toward idle/low values
        if (MotionSensors.Contains(sensorName) || CurrentSensors.Contains(sensorName))
            return Clamp(NextGaussian(0, 1), 0, max * 0.1);

        return Clamp(NextGaussian(midpoint * (1 - degradation * 0.3), range * 0.05), min * 0.7, max);
    }

    private static bool IsFailureTrigger(string sensorName, string reason) => reason switch
    {
        "BearingFailure"  => sensorName == "MotorCurrent",
        "HydraulicLeak"   => sensorName == "HydraulicPressure",
        "DriveFailure"    => MotionSensors.Contains(sensorName),
        "MotorOverload"   => sensorName == "MotorCurrent",
        "PowerOutage"     => MotionSensors.Contains(sensorName) || CurrentSensors.Contains(sensorName),
        "RubberStick"     => sensorName == "MotorCurrent",
        "TempOutOfRange"  => sensorName is "ChamberTemp" or "FrontRollTemp" or "BackRollTemp" or "ExitCompoundTemp",
        "CoolingFailure"  => sensorName == "WaterFlowRate",
        "EmergencyStop"   => MotionSensors.Contains(sensorName) || CurrentSensors.Contains(sensorName),
        "SafetyGuardOpen" => MotionSensors.Contains(sensorName) || CurrentSensors.Contains(sensorName),
        _ => false
    };

    private static bool GoesHigh(string sensorName, string reason) => reason switch
    {
        "BearingFailure" => true,  // current spikes
        "MotorOverload"  => true,  // current spikes
        "RubberStick"    => true,  // current spikes
        "TempOutOfRange" => true,  // temp drifts high
        _ => false                  // everything else drops low
    };

    private static IEnumerable<DateTime> EnumerateSlots(DateTime from, DateTime to)
    {
        var cursor = from;
        while (cursor < to)
        {
            yield return cursor;
            cursor = cursor.AddMinutes(IntervalMinutes);
        }
    }

    private double NextGaussian(double mean, double stdDev)
    {
        var u1 = 1.0 - rng.NextDouble();
        var u2 = 1.0 - rng.NextDouble();
        var z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stdDev * z;
    }

    private static double Clamp(double value, double min, double max) =>
        Math.Max(min, Math.Min(max, value));
}
