namespace PhysioAssist.Api.Modules.InitialReportModule.Errors;

public static class TreatmentSchedulePlanErrors
{
    public static readonly Error NotFound = new(
        "TreatmentSchedulePlan.NotFound",
        "No schedule requirements section found for this report.",
        404);

    public static readonly Error InvalidTotalSessions = new(
        "TreatmentSchedulePlan.InvalidTotalSessions",
        "Total sessions must be greater than zero.",
        400);

    public static readonly Error InvalidSessionDuration = new(
        "TreatmentSchedulePlan.InvalidSessionDuration",
        "Session duration must be greater than zero.",
        400);

    public static readonly Error AlreadyResolved = new(
        "TreatmentSchedulePlan.AlreadyResolved",
        "This schedule requirement has already been booked or sent to the receptionist.",
        409);
}
