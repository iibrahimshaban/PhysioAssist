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

            // Single AND multiple named weekdays now both live under SpecificWeekdays,
            // distinguished only by how many bits are set in PreferredWeekdays.
            RelativeDayToken.SpecificWeekdays => ResolveSpecificWeekdaysRange(preference.PreferredWeekdays, today),

            _ => (today, today.AddDays(7)) // Unspecified — default one-week search window
        };
    }

    private static (DateOnly Start, DateOnly End) ResolveSpecificWeekdaysRange(DaysOfWeekFlags weekdays, DateOnly today)
    {
        if (weekdays == DaysOfWeekFlags.None)
            // Shouldn't normally happen (parser should never emit SpecificWeekdays with
            // no bits set), but fail safe with the same default window as Unspecified.
            return (today, today.AddDays(7));

        // Exactly one weekday requested — collapse to that single next occurrence,
        // same behavior the old dedicated Sunday/Monday/etc. cases used to give.
        if (IsSingleFlag(weekdays))
        {
            var target = ToDayOfWeek(weekdays);
            var date = NextOccurrence(today, target);
            return (date, date);
        }

        // Multiple weekdays requested (e.g. "Saturday, Monday, Wednesday") — can't
        // collapse to one date, so widen to a 14-day window. The caller filters the
        // resulting slots down to only the requested weekdays.
        return (today, today.AddDays(14));
    }

    private static bool IsSingleFlag(DaysOfWeekFlags flags) => (flags & (flags - 1)) == 0;

    private static DayOfWeek ToDayOfWeek(DaysOfWeekFlags flag) => flag switch
    {
        DaysOfWeekFlags.Sunday => DayOfWeek.Sunday,
        DaysOfWeekFlags.Monday => DayOfWeek.Monday,
        DaysOfWeekFlags.Tuesday => DayOfWeek.Tuesday,
        DaysOfWeekFlags.Wednesday => DayOfWeek.Wednesday,
        DaysOfWeekFlags.Thursday => DayOfWeek.Thursday,
        DaysOfWeekFlags.Friday => DayOfWeek.Friday,
        DaysOfWeekFlags.Saturday => DayOfWeek.Saturday,
        _ => throw new ArgumentOutOfRangeException(nameof(flag), flag, "Not a single-day flag.")
    };

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