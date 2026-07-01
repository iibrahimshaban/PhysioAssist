namespace PhysioAssist.Api.Modules.Intake.Errors;

public static class IntakeErrors
{
    public static readonly Error SchemaNotFound = new(
        "Intake.SchemaNotFound",
        "The requested intake form schema was not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error IntakeNotFound = new(
        "Intake.IntakeNotFound",
        "The requested intake submission was not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error UnauthorizedDoctor = new(
        "Intake.UnauthorizedDoctor",
        "The current doctor is not allowed to access this intake resource.",
        StatusCodes.Status403Forbidden);

    public static readonly Error InvalidSchema = new(
        "Intake.InvalidSchema",
        "The intake form schema is invalid.",
        StatusCodes.Status400BadRequest);

    public static readonly Error InvalidSubmission = new(
        "Intake.InvalidSubmission",
        "The intake form submission is invalid.",
        StatusCodes.Status400BadRequest);

    public static readonly Error SchemaNameDuplicated = new(
        "Intake.SchemaNameDuplicated",
        "A form schema with this name already exists for the doctor.",
        StatusCodes.Status409Conflict);

    public static readonly Error SchemaNotPublished = new(
        "Intake.SchemaNotPublished",
        "The form schema must be published before generating QR links or accepting submissions.",
        StatusCodes.Status400BadRequest);

    public static readonly Error InvalidStatusTransition = new(
        "Intake.InvalidStatusTransition",
        "The requested status transition is not allowed.",
        StatusCodes.Status400BadRequest);

    public static readonly Error AlreadyConverted = new(
        "Intake.AlreadyConverted",
        "This intake has already been converted to a patient record.",
        StatusCodes.Status409Conflict);

    public static readonly Error ConversionDependencyMissing = new(
        "Intake.ConversionDependencyMissing",
        "Patient conversion service is not available. Cannot convert intake to patient.",
        StatusCodes.Status503ServiceUnavailable);

    public static readonly Error SubmissionNotFound = new(
        "Intake.SubmissionNotFound",
        "The requested intake submission was not found.",
        StatusCodes.Status404NotFound);
}
