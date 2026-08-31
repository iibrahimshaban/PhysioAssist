namespace PhysioAssist.Api.Modules.PackageModule.Entities;

public class TreatmentSchedulePlan : AuditableEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ReportId { get; set; }

    // --- Mandatory ---
    public int TotalSessions { get; set; }
    public int SessionDurationMinutes { get; set; }

    // --- Optional, with safety defaults (see PatientSessionPackage on the Scheduling side) ---
    public int SessionsPerWeek { get; set; } = 3;
    public int MinimumGapBetweenSessionsDays { get; set; } = 2;
    public SchedulingPriority Priority { get; set; } = SchedulingPriority.Normal;

    // --- Lifecycle ---
    public TreatmentSchedulePlanStatus LifeCycleStatus { get; set; } = TreatmentSchedulePlanStatus.Pending;
    public bool AllowSameDayBooking { get; set; } = false;
}
