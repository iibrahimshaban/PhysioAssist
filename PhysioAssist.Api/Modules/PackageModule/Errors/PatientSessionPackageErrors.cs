namespace PhysioAssist.Api.Modules.PackageModule.Errors;

public static class PatientSessionPackageErrors
{
    public static readonly Error InvalidTotalSessions = new(
        "PatientSessionPackage.InvalidTotalSessions",
        "Total sessions must be greater than zero.",
        400);
    public static readonly Error NotFound = new(
        "PatientSessionPackage.NotFound",
        "Package not found.",
        404);

    public static readonly Error PackageAlreadyStopped = new(
        "Package.AlreadyStopped", "This package has already been stopped.",StatusCodes.Status409Conflict);
}
