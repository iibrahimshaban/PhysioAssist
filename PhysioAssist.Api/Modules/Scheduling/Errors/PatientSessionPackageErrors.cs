namespace PhysioAssist.Api.Modules.Scheduling.Errors;

public static class PatientSessionPackageErrors
{
    public static readonly Error InvalidTotalSessions = new(
        "PatientSessionPackage.InvalidTotalSessions",
        "Total sessions must be greater than zero.",
        400);
}
