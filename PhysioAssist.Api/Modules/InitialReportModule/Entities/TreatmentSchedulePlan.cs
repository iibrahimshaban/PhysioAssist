using PhysioAssist.Api.Modules.Scheduling.DTO.AgentDtos;
using PhysioAssist.Api.Shared.Dtos.Schedule;

namespace PhysioAssist.Api.Modules.InitialReportModule.Entities;

public class TreatmentSchedulePlan : AuditableEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ReportId { get; set; }
    public InitialReport Report { get; set; } = default!;

    // --- Mandatory ---
    public int TotalSessions { get; set; }
    public int SessionDurationMinutes { get; set; }

    // --- Optional, with safety defaults (see PatientSessionPackage on the Scheduling side) ---
    public int SessionsPerWeek { get; set; } = 3;
    public int MinimumGapBetweenSessionsDays { get; set; } = 2;
    public SchedulingPriority Priority { get; set; } = SchedulingPriority.Normal;

    // --- Lifecycle ---
    public TreatmentSchedulePlanStatus Status { get; set; } = TreatmentSchedulePlanStatus.Pending;
    public bool AllowSameDayBooking { get; set; } = false;

    /// <summary>Set only once Status is Booked — links back to Scheduling's PatientSessionPackage.</summary>
    public Guid? PackageId { get; set; }
}
