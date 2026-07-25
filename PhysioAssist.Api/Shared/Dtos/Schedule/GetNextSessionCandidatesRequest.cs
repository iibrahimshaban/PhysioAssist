namespace PhysioAssist.Api.Shared.Dtos.Schedule;

public record GetNextSessionCandidatesRequest(
    string? PatientFreeTimeOverride,
    bool PersistFreeTimeOverride,
    TimeSpan? SessionDurationOverride,
    int? SessionsPerWeekOverride,
    int? MinimumGapOverrideDays,
    PreferredTimeOfDay? PreferredTimeOfDayOverride,
    DaysOfWeekFlags? PreferredDaysOverride
    );
