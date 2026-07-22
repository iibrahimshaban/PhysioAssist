namespace PhysioAssist.Api.Modules.Scheduling.helpers;

public static class TimePreferenceResolver
{
    public static (DateOnly Start, DateOnly End) ResolveDateRange(PatientTimePreferenceDto preference, DateOnly today)
    {
        if (preference.ExplicitDate is { } explicitDate)
            return (explicitDate, explicitDate);

        return preference.DayToken switch
        {
            RelativeDayToken.Today => (today, today),
            RelativeDayToken.Tomorrow => (today.AddDays(1), today.AddDays(1)),
            RelativeDayToken.DayAfterTomorrow => (today.AddDays(2), today.AddDays(2)),
            RelativeDayToken.ThisWeek => (today, EndOfWeek(today)),
            RelativeDayToken.NextWeek => (StartOfNextWeek(today), EndOfWeek(StartOfNextWeek(today))),
            RelativeDayToken.Sunday => Single(NextOccurrence(today, DayOfWeek.Sunday)),
            RelativeDayToken.Monday => Single(NextOccurrence(today, DayOfWeek.Monday)),
            RelativeDayToken.Tuesday => Single(NextOccurrence(today, DayOfWeek.Tuesday)),
            RelativeDayToken.Wednesday => Single(NextOccurrence(today, DayOfWeek.Wednesday)),
            RelativeDayToken.Thursday => Single(NextOccurrence(today, DayOfWeek.Thursday)),
            RelativeDayToken.Friday => Single(NextOccurrence(today, DayOfWeek.Friday)),
            RelativeDayToken.Saturday => Single(NextOccurrence(today, DayOfWeek.Saturday)),
            _ => (today, today.AddDays(7)) // Unspecified — default one-week search window
        };
    }

    private static (DateOnly, DateOnly) Single(DateOnly date) => (date, date);

    private static DateOnly NextOccurrence(DateOnly from, DayOfWeek target)
    {
        var daysUntil = ((int)target - (int)from.DayOfWeek + 7) % 7;
        daysUntil = daysUntil == 0 ? 7 : daysUntil; // "Sunday" said on a Sunday means next Sunday, not today
        return from.AddDays(daysUntil);
    }

    private static DateOnly EndOfWeek(DateOnly date)
    {
        // Week ends Saturday — matches AppointmentService.ResolveRange's Sunday-start convention.
        var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)date.DayOfWeek + 7) % 7;
        return date.AddDays(daysUntilSaturday);
    }

    private static DateOnly StartOfNextWeek(DateOnly date)
    {
        var daysUntilNextSunday = ((int)DayOfWeek.Sunday - (int)date.DayOfWeek + 7) % 7;
        daysUntilNextSunday = daysUntilNextSunday == 0 ? 7 : daysUntilNextSunday;
        return date.AddDays(daysUntilNextSunday);
    }
}
