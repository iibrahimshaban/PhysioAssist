namespace PhysioAssist.Api.Modules.Scheduling.Errors;

public static class SchedulingErrors
{
    public static readonly Error PackageNotFound = new(
        "SchedulingPackage.NotFound",
        "No session package found with the given id.",
        StatusCodes.Status404NotFound);

    public static readonly Error PackageNotActive = new(
        "SchedulingPackage.NotActive",
        "The selected session package is not active.",
        StatusCodes.Status400BadRequest);

    public static readonly Error SlotNotFound = new(
        "SchedulingSlot.NotFound",
        "No session slot found with the given id.",
        StatusCodes.Status404NotFound);

    public static readonly Error PackageAlreadyComplete = new(
        "SchedulingPackage.AlreadyComplete",
        "All sessions in this package have already been scheduled.",
        StatusCodes.Status409Conflict);

    public static readonly Error DoctorHasNoActiveSchedule = new(
        "SchedulingPackage.DoctorHasNoActiveSchedule",
        "The selected doctor has no active schedule available.",
        StatusCodes.Status400BadRequest);

    public static readonly Error SlotNotInBookedState = new(
        "SchedulingSlot.NotInBookedState",
        "The selected slot is not in the booked state.",
        StatusCodes.Status400BadRequest);
}
