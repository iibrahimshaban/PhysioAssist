namespace PhysioAssist.Api.Shared.Dtos.Patient;

public sealed record PatientTimePreferenceInfo(
    RelativeDayToken DayToken,
    DateOnly? ExplicitDate,
    List<PatientPreferredTimeGroupDto> Groups
    );
