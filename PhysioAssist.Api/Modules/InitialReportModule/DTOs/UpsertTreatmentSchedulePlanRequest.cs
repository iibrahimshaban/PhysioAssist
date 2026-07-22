using PhysioAssist.Api.Modules.Scheduling.DTO.AgentDtos;

namespace PhysioAssist.Api.Modules.InitialReportModule.DTOs;

public record UpsertTreatmentSchedulePlanRequest(
        int TotalSessions,
        int SessionDurationMinutes,
        int SessionsPerWeek,
        int MinimumGapBetweenSessionsDays,
        PreferredTimeOfDay PreferredTimeOfDay,
        DaysOfWeekFlags PreferredDays,
        SchedulingPriority Priority
    );
