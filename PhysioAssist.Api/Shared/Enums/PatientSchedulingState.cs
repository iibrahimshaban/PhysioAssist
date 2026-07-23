namespace PhysioAssist.Api.Shared.Enums;

public enum PatientSchedulingState
{
    NoInitialReport,   // patient has no report yet at all
    PlanPending,       // doctor hasn't finished deciding (Status = Pending)
    ReadyToSchedule,   // Status = SentToReceptionist, PackageId is null
    ActivePackage      // Status = Booked, PackageId is set
}
