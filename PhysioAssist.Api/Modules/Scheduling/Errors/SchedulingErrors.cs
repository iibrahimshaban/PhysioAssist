namespace PhysioAssist.Api.Modules.Scheduling.Errors;

public static class SchedulingErrors
{
    public static readonly Error PackageNotFound = new(
        "SchedulingPackage.NotFound",
        "No session package found with the given id.",
        StatusCodes.Status404NotFound);

    public static readonly Error PackageAlreadyComplete = new(
        "SchedulingPackage.AlreadyComplete",
        "All sessions in this package have already been scheduled.",
        StatusCodes.Status409Conflict);
}
