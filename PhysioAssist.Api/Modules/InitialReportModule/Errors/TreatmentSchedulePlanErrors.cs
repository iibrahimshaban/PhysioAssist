namespace PhysioAssist.Api.Modules.InitialReportModule.Errors;

public static class TreatmentSchedulePlanErrors
{
    public static readonly Error NotFound = new(
        "TreatmentSchedulePlan.NotFound",
        "No schedule requirements section found for this report.",
        StatusCodes.Status400BadRequest);

    public static readonly Error InvalidTotalSessions = new(
        "TreatmentSchedulePlan.InvalidTotalSessions",
        "Total sessions must be greater than zero.",
        StatusCodes.Status400BadRequest);

    public static readonly Error InvalidSessionDuration = new(
        "TreatmentSchedulePlan.InvalidSessionDuration",
        "Session duration must be greater than zero.",
        StatusCodes.Status400BadRequest);

    public static readonly Error AlreadyResolved = new(
        "TreatmentSchedulePlan.AlreadyResolved",
        "This schedule requirement has already been booked or sent to the receptionist.",
        StatusCodes.Status409Conflict);

    public static readonly Error AccessDenied = new(
        "TreatmentSchedulePlan.AccessDenied",
        "This treatment plan does not belong to your managing doctor.",
        StatusCodes.Status403Forbidden);
}
