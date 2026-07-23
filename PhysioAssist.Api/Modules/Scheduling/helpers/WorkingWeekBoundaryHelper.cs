namespace PhysioAssist.Api.Modules.Scheduling.helpers;

public static class WorkingWeekBoundaryHelper
{
    /// <summary>
    /// Given the start of a 7-day cycle and the doctor's set of recurring working
    /// weekdays (from WorkingSchedule/WorkingScheduleDay), returns the LAST date in
    /// that cycle that the doctor actually works — not just cycleStart+6. Walks
    /// backward from day 6 so a doctor who, say, only works Sun/Tue/Thu ends up with
    /// a week that closes on whichever of those falls last in the window, instead of
    /// assuming a fixed calendar week.
    /// </summary>
    public static DateOnly GetCycleEnd(DateOnly cycleStart, IReadOnlySet<DayOfWeek> workingDays)
    {
        if (workingDays.Count == 0)
            return cycleStart.AddDays(6); // no schedule data — fall back to a plain 7-day span

        for (var offset = 6; offset >= 0; offset--)
        {
            var candidate = cycleStart.AddDays(offset);
            if (workingDays.Contains(candidate.DayOfWeek))
                return candidate;
        }

        // Doctor has no working day at all inside this cycle (schedule gap wider
        // than a week) — return a date before cycleStart so callers can detect
        // "nothing bookable this cycle" via weekEnd < weekStart.
        return cycleStart.AddDays(-1);
    }
}
