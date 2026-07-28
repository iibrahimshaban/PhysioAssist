namespace PhysioAssist.Api.Modules.Scheduling.DTO;

public record DoctorSchedulingPreferenceDto(
    Guid Id,
    Guid DoctorId,
    int MaxShortfallToleranceMinutes,
    int MaxDaysOutForExactMatch,
    bool AllowShorterSlots
    );
