using Dapper;
using KPIDash.Data.Models;

namespace KPIDash.Data.Repositories;

public class KpiRepository(DbConnectionFactory connectionFactory) : IKpiRepository
{
    public async Task<KpiSummary?> GetAsync(string shiftDate, string shift, string line)
    {
        var (shiftStart, shiftEnd) = GetShiftWindow(shiftDate, shift);
        var startStr = shiftStart.ToString("o");
        var endStr   = shiftEnd.ToString("o");

        using var conn = connectionFactory.Create();

        // --- Planned production time, target batches, planned operators ---
        var sched = await conn.QuerySingleOrDefaultAsync<(double PlannedMinutes, int TargetBatches, int PlannedOperators)>(
            """
            SELECT
                COALESCE(SUM((julianday(ScheduledEnd) - julianday(ScheduledStart)) * 1440), 0) AS PlannedMinutes,
                COALESCE(SUM(TargetBatches), 0) AS TargetBatches,
                COALESCE(MAX(PlannedOperators), 0) AS PlannedOperators
            FROM ProductionSchedule
            WHERE ShiftDate = @ShiftDate AND Shift = @Shift AND Line = @Line
            """,
            new { ShiftDate = shiftDate, Shift = shift, Line = line });

        if (sched.PlannedMinutes == 0)
            return null;

        // --- Banbury (InternalMixer) downtime for this line within the shift window ---
        // Ongoing events (EndedAt IS NULL) use elapsed time from StartedAt to now.
        var downtimeMinutes = await conn.QuerySingleAsync<double>(
            """
            SELECT COALESCE(SUM(
                CASE
                    WHEN d.DurationMinutes IS NOT NULL THEN d.DurationMinutes
                    WHEN d.EndedAt IS NULL THEN (julianday('now') - julianday(d.StartedAt)) * 1440
                    ELSE 0
                END
            ), 0)
            FROM DowntimeEvents d
            JOIN Equipment e ON e.EquipmentId = d.EquipmentId
            WHERE e.Type = 'InternalMixer'
              AND e.Name LIKE @LinePattern
              AND d.StartedAt >= @Start
              AND d.StartedAt < @End
            """,
            new { LinePattern = $"{line}%", Start = startStr, End = endStr });

        // --- Idle time for InternalMixer within the shift window ---
        // EquipmentStatus is event-sourced: each row marks when a status began.
        // LEAD() derives when it ended; clip windows to [shiftStart, shiftEnd].
        // Include the last status before the shift to handle carry-over idle.
        var idleMinutes = await conn.QuerySingleAsync<double>(
            """
            WITH status_windows AS (
                SELECT es.Status,
                       es.RecordedAt AS WinStart,
                       LEAD(es.RecordedAt) OVER (ORDER BY es.RecordedAt) AS WinEnd
                FROM EquipmentStatus es
                JOIN Equipment e ON e.EquipmentId = es.EquipmentId
                WHERE e.Type = 'InternalMixer'
                  AND e.Name LIKE @LinePattern
                  AND es.RecordedAt >= COALESCE(
                      (SELECT MAX(es2.RecordedAt)
                       FROM EquipmentStatus es2
                       JOIN Equipment e2 ON e2.EquipmentId = es2.EquipmentId
                       WHERE e2.Type = 'InternalMixer'
                         AND e2.Name LIKE @LinePattern
                         AND es2.RecordedAt < @Start),
                      @Start
                  )
                  AND es.RecordedAt < @End
            )
            SELECT COALESCE(SUM(
                (julianday(
                    CASE WHEN COALESCE(WinEnd, @End) > @End THEN @End ELSE COALESCE(WinEnd, @End) END
                ) - julianday(
                    CASE WHEN WinStart < @Start THEN @Start ELSE WinStart END
                )) * 1440
            ), 0)
            FROM status_windows
            WHERE Status = 'Idle'
              AND (CASE WHEN WinStart < @Start THEN @Start ELSE WinStart END) <
                  (CASE WHEN COALESCE(WinEnd, @End) > @End THEN @End ELSE COALESCE(WinEnd, @End) END)
            """,
            new { LinePattern = $"{line}%", Start = startStr, End = endStr });

        // --- Batch counts ---
        var batches = await conn.QuerySingleAsync<(int Total, int Good)>(
            """
            SELECT
                COUNT(*)                                                        AS Total,
                SUM(CASE WHEN Status = 'Complete' THEN 1 ELSE 0 END)           AS Good
            FROM Batches
            WHERE Line = @Line
              AND StartedAt >= @Start
              AND StartedAt < @End
            """,
            new { Line = line, Start = startStr, End = endStr });

        // --- Actual operators clocked in for this shift + line ---
        var actualOperators = await conn.QuerySingleAsync<int>(
            """
            SELECT COUNT(DISTINCT EmployeeId)
            FROM TimeLog
            WHERE ShiftDate = @ShiftDate AND Shift = @Shift AND Line = @Line
            """,
            new { ShiftDate = shiftDate, Shift = shift, Line = line });

        // --- KPI calculations ---
        var planned  = sched.PlannedMinutes;
        var downtime = Math.Min(downtimeMinutes, planned);
        var idle     = Math.Min(idleMinutes, planned - downtime);

        var availability = planned > 0 ? (planned - downtime - idle) / planned : 0.0;

        var target      = sched.TargetBatches;
        var actual      = batches.Total;
        var performance = target > 0 ? (double)actual / target : 0.0;

        var good    = batches.Good;
        var total   = batches.Total;
        var quality = total > 0 ? (double)good / total : 0.0;

        var oee = availability * performance * quality;

        var bpo = actualOperators > 0 ? (double)actual / actualOperators : 0.0;

        // Labour Efficiency = (Target BPO) / (Actual BPO)
        //   Target BPO  = TargetBatches / PlannedOperators
        //   Actual BPO  = ActualBatches / ActualOperators
        var targetBpo       = sched.PlannedOperators > 0 ? (double)target / sched.PlannedOperators : 0.0;
        var labourEfficiency = bpo > 0 ? Math.Min(1.0, targetBpo / bpo) : 0.0;

        return new KpiSummary
        {
            Line                    = line,
            ShiftDate               = shiftDate,
            Shift                   = shift,
            PlannedProductionMinutes = planned,
            DowntimeMinutes         = downtime,
            IdleMinutes             = idle,
            TargetBatches           = target,
            ActualBatches           = actual,
            GoodBatches             = good,
            TotalBatches            = total,
            PlannedOperators        = sched.PlannedOperators,
            ActualOperators         = actualOperators,
            Availability            = availability,
            Performance             = performance,
            Quality                 = quality,
            Oee                     = oee,
            BatchesPerOperator      = bpo,
            LabourEfficiency        = labourEfficiency,
        };
    }

    private static (DateTime Start, DateTime End) GetShiftWindow(string shiftDate, string shift)
    {
        var date = DateTime.Parse(shiftDate,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);

        return shift switch
        {
            "Day"       => (date.AddHours(6),  date.AddHours(14)),
            "Afternoon" => (date.AddHours(14), date.AddHours(22)),
            "Night"     => (date.AddHours(22), date.AddDays(1).AddHours(6)),
            _           => throw new ArgumentException($"Unknown shift: {shift}"),
        };
    }
}
