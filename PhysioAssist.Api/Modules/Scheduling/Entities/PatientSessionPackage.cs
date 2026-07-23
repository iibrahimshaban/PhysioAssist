using PhysioAssist.Api.Modules.Scheduling.DTO.AgentDtos;

namespace PhysioAssist.Api.Modules.Scheduling.Entities;

public class PatientSessionPackage : AuditableEntity
{
    public Guid Id { get; set; }

    public Guid PatientId { get; set; }

    public Guid DoctorId { get; set; }

    // --- Mandatory (set when the treatment plan is created) ---
    public int TotalSessions { get; set; }
    public TimeSpan SessionDuration { get; set; }

    // --- Tracking ---
    public int ScheduledSessions { get; set; }
    public int RemainingSessions { get; set; }
    public PackageStatus Status { get; set; } = PackageStatus.Active;
    public int SessionsPerWeek { get; set; } = 3;
    public int MinimumGapBetweenSessionsDays { get; set; } = 2;
    public PreferredTimeOfDay PreferredTimeOfDay { get; set; } = PreferredTimeOfDay.Unspecified;
    public DaysOfWeekFlags PreferredDays { get; set; } = DaysOfWeekFlags.None;
    public SchedulingPriority Priority { get; set; } = SchedulingPriority.Normal; // informational only for now
}
