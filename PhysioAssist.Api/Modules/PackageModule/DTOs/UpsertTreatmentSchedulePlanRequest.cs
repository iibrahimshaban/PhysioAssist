using PhysioAssist.Api.Shared.Dtos.Schedule;

namespace PhysioAssist.Api.Modules.PackageModule.DTOs;

public record UpsertTreatmentSchedulePlanRequest(
        int TotalSessions,
        int SessionDurationMinutes,
        int SessionsPerWeek,
        int MinimumGapBetweenSessionsDays,
        SchedulingPriority Priority,
        bool AllowSameDayBooking = false
    );
