using PhysioAssist.Api.Shared.Dtos.Schedule;

namespace PhysioAssist.Api.Modules.PackageModule.DTOs;

public record TreatmentSchedulePlanResponse(
        Guid Id,
        Guid ReportId,
        int TotalSessions,
        int SessionDurationMinutes,
        int SessionsPerWeek,
        int MinimumGapBetweenSessionsDays,
        SchedulingPriority Priority,
        TreatmentSchedulePlanStatus Status,
        bool AllowSameDayBooking,
        IReadOnlyList<SlotCandidateDto> CandidateSlots
    );
