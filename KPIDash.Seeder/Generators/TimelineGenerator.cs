namespace KPIDash.Seeder.Generators;

public class TimelineGenerator(Random rng)
{
    // Downtime reasons valid for each equipment type
    private static readonly Dictionary<string, string[]> DownReasons = new()
    {
        ["Conveyor"]      = ["BearingFailure", "MotorOverload", "EmergencyStop", "SafetyGuardOpen"],
        ["InternalMixer"] = ["HydraulicLeak", "RubberStick", "TempOutOfRange", "MotorOverload", "BearingFailure"],
        ["Mill"]          = ["BearingFailure", "TempOutOfRange", "DriveFailure", "MotorOverload"],
        ["CoolingLine"]   = ["CoolingFailure", "BearingFailure", "DriveFailure"],
    };

    public List<StateWindow> Generate(string equipmentType, DateTime from, DateTime to)
    {
        var downtimes = ScheduleDowntimes(equipmentType, from, to);

        var windows = new List<StateWindow>();
        var cursor = from;

        foreach (var (start, end, reason) in downtimes.OrderBy(d => d.Start))
        {
            if (cursor < start)
                FillRunningIdle(windows, cursor, start);

            windows.Add(new StateWindow(start, end, "Down", reason));
            cursor = end;
        }

        if (cursor < to)
            FillRunningIdle(windows, cursor, to);

        return windows;
    }

    // Shift definitions: (startHour, endHour) where endHour > 24 means next day
    private static readonly (int StartHour, int EndHour)[] ShiftDefs = [(6, 14), (14, 22), (22, 30)];

    private List<(DateTime Start, DateTime End, string Reason)> ScheduleDowntimes(
        string equipmentType, DateTime from, DateTime to)
    {
        var reasons = DownReasons[equipmentType];
        var result = new List<(DateTime, DateTime, string)>();

        // Guarantee exactly 1 downtime event per shift window
        for (var day = from.Date; day <= to.Date; day = day.AddDays(1))
        {
            foreach (var (startHour, endHour) in ShiftDefs)
            {
                var shiftStart = day.AddHours(startHour);
                var shiftEnd   = day.AddHours(endHour); // endHour=30 → next day 06:00

                if (shiftStart >= to) break;
                if (shiftEnd > to) shiftEnd = to;

                var shiftMinutes = (shiftEnd - shiftStart).TotalMinutes;
                if (shiftMinutes < 30) continue;

                // Place downtime in the middle 70% of the shift window
                var padding   = shiftMinutes * 0.15;
                var available = shiftMinutes - padding * 2;
                var offset    = rng.NextDouble() * available + padding;
                var dtStart   = shiftStart.AddMinutes(offset);
                var duration  = rng.Next(15, 61); // 15-60 min — fits within one shift
                var dtEnd     = dtStart.AddMinutes(duration);
                if (dtEnd > shiftEnd) dtEnd = shiftEnd;

                var reason = reasons[rng.Next(reasons.Length)];
                result.Add((dtStart, dtEnd, reason));
            }
        }

        // Remove overlaps: shift later event forward if it starts before the previous ends
        result.Sort((a, b) => a.Item1.CompareTo(b.Item1));
        for (int i = 1; i < result.Count; i++)
        {
            if (result[i].Item1 < result[i - 1].Item2)
            {
                var shifted = result[i - 1].Item2.AddMinutes(10);
                var dur = (result[i].Item2 - result[i].Item1).TotalMinutes;
                result[i] = (shifted, shifted.AddMinutes(dur), result[i].Item3);
            }
        }

        return result.Where(d => d.Item1 < to && d.Item2 <= to).ToList();
    }

    public List<StateWindow> ApplyCascade(List<StateWindow> downstream, List<StateWindow> upstream)
    {
        var points = new SortedSet<DateTime>();
        foreach (var w in downstream.Concat(upstream))
        {
            points.Add(w.Start);
            points.Add(w.End);
        }

        var segments = new List<StateWindow>();
        var pointList = points.ToList();

        for (int i = 0; i < pointList.Count - 1; i++)
        {
            var segStart = pointList[i];
            var segEnd   = pointList[i + 1];

            var downW = FindWindowAt(downstream, segStart);
            if (downW == null) continue;

            var upW = FindWindowAt(upstream, segStart);
            var upRunning = upW?.Status == "Running";

            var status = downW.Status;
            var reason = downW.Reason;

            if (!upRunning && status == "Running")
            {
                status = "Idle";
                reason = "Idle";
            }

            segments.Add(new StateWindow(segStart, segEnd, status, reason));
        }

        return CollapseShortSegments(MergeWindows(segments), minMinutes: 2.0);
    }

    private static List<StateWindow> CollapseShortSegments(List<StateWindow> windows, double minMinutes)
    {
        var list = windows.ToList();
        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int i = 0; i < list.Count; i++)
            {
                if ((list[i].End - list[i].Start).TotalMinutes >= minMinutes) continue;
                if (i < list.Count - 1)
                    list[i + 1] = list[i + 1] with { Start = list[i].Start };
                else if (i > 0)
                    list[i - 1] = list[i - 1] with { End = list[i].End };
                list.RemoveAt(i);
                changed = true;
                break;
            }
        }
        return MergeWindows(list);
    }

    private static StateWindow? FindWindowAt(List<StateWindow> windows, DateTime time) =>
        windows.FirstOrDefault(w => w.Start <= time && w.End > time);

    private static List<StateWindow> MergeWindows(List<StateWindow> windows)
    {
        if (windows.Count == 0) return windows;
        var merged = new List<StateWindow> { windows[0] };
        foreach (var curr in windows.Skip(1))
        {
            var last = merged[^1];
            if (last.Status == curr.Status && last.Reason == curr.Reason && last.End == curr.Start)
                merged[^1] = last with { End = curr.End };
            else
                merged.Add(curr);
        }
        return merged;
    }

    private void FillRunningIdle(List<StateWindow> windows, DateTime start, DateTime end)
    {
        var cursor = start;
        while (cursor < end)
        {
            // Running: 8-12 minutes (one batch cycle)
            var runMinutes = rng.Next(8, 13);
            var runEnd = cursor.AddMinutes(runMinutes);
            if (runEnd >= end)
            {
                windows.Add(new StateWindow(cursor, end, "Running", "NormalOperation"));
                return;
            }
            windows.Add(new StateWindow(cursor, runEnd, "Running", "NormalOperation"));
            cursor = runEnd;

            // Idle: 1-2 minutes between batches
            var idleMinutes = rng.Next(1, 3);
            var idleEnd = cursor.AddMinutes(idleMinutes);
            if (idleEnd >= end)
            {
                windows.Add(new StateWindow(cursor, end, "Idle", "Idle"));
                return;
            }
            windows.Add(new StateWindow(cursor, idleEnd, "Idle", "Idle"));
            cursor = idleEnd;
        }
    }
}
