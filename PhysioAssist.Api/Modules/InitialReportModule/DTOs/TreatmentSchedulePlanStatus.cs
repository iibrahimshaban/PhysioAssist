namespace PhysioAssist.Api.Modules.InitialReportModule.DTOs;

public enum TreatmentSchedulePlanStatus
{
    /// <summary>Filled out by the doctor; no booking decision made yet.</summary>
    Pending = 0,

    /// <summary>Doctor deferred booking — receptionist will collect free time and book later.</summary>
    SentToReceptionist = 1,

    /// <summary>PatientSessionPackage + first ScheduleSlot exist in Scheduling module — see PackageId.</summary>
    Booked = 2
}
