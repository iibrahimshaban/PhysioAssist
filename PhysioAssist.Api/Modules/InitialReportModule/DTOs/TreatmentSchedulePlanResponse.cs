using PhysioAssist.Api.Modules.Scheduling.DTO.AgentDtos;
using PhysioAssist.Api.Shared.Dtos.Schedule;

namespace PhysioAssist.Api.Modules.InitialReportModule.DTOs;

public record TreatmentSchedulePlanResponse(
        Guid Id,
        Guid ReportId,
        int TotalSessions,
        int SessionDurationMinutes,
        int SessionsPerWeek,
        int MinimumGapBetweenSessionsDays,
        PreferredTimeOfDay PreferredTimeOfDay,
        DaysOfWeekFlags PreferredDays,
        SchedulingPriority Priority,
        TreatmentSchedulePlanStatus Status,
        Guid? PackageId,
        IReadOnlyList<SlotCandidateDto> CandidateSlots
    );
