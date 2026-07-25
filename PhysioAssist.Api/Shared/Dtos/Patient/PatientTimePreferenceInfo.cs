namespace PhysioAssist.Api.Shared.Dtos.Patient;

public sealed record PatientTimePreferenceInfo(
    RelativeDayToken DayToken,
    DaysOfWeekFlags PreferredWeekdays,
    TimeOnly? PreferredTimeFrom,
    TimeOnly? PreferredTimeTo
    );
