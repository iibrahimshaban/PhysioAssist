namespace PhysioAssist.Api.Shared.Dtos.Schedule;

public class PatientSchedulingContextDto
{
    public required PatientSchedulingState State { get; init; }
    public PendingTreatmentPlanDto? PendingPlan { get; init; }       // set only when ReadyToSchedule
    public PatientSessionPackageSummaryDto? ActivePackage { get; init; } // set only when ActivePackage
}
