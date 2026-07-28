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

    public static readonly Error SubmissionNotFound = new(
        "Intake.SubmissionNotFound",
        "The requested intake submission was not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error LockedQuestionRemoved = new(
        "Intake.LockedQuestionRemoved",
        "Locked questions cannot be removed.",
        StatusCodes.Status400BadRequest);

    public static readonly Error CopyLimitExceeded = new(
        "Intake.CopyLimitExceeded",
        "Maximum number of copies per form has been reached.",
        StatusCodes.Status400BadRequest);

    public static readonly Error CannotDeleteDefaultSchema = new(
        "Intake.CannotDeleteDefaultSchema",
        "Default form schema cannot be deleted.",
        StatusCodes.Status400BadRequest);

    public static readonly Error FormSchemaInUse = new(
        "Intake.FormSchemaInUse",
        "This form has associated submissions and cannot be deleted. Archive it instead.",
        StatusCodes.Status409Conflict);

    public static readonly Error SchemaAlreadyArchived = new(
        "Intake.SchemaAlreadyArchived",
        "Form schema is already archived.",
        StatusCodes.Status400BadRequest);

    public static readonly Error SchemaNotArchived = new(
        "Intake.SchemaNotArchived",
        "Form schema is not archived.",
        StatusCodes.Status400BadRequest);

    // ─── Core / Minimum Required Field Errors ───────────────

    public static readonly Error CoreFieldMissing = new(
        "Intake.CoreFieldMissing",
        "One or more required core fields are missing from the schema.",
        StatusCodes.Status400BadRequest);

    public static readonly Error CoreFieldTypeChanged = new(
        "Intake.CoreFieldTypeChanged",
        "The type of a locked core field cannot be changed.",
        StatusCodes.Status400BadRequest);

    public static readonly Error CoreFieldRequiredFlagChanged = new(
        "Intake.CoreFieldRequiredFlagChanged",
        "The required flag of a locked core field cannot be disabled.",
        StatusCodes.Status400BadRequest);

    public static readonly Error RequiredFieldEmpty = new(
        "Intake.RequiredFieldEmpty",
        "One or more required fields have empty values in the submission.",
        StatusCodes.Status400BadRequest);

    public static readonly Error PublishValidationFailed = new(
        "Intake.PublishValidationFailed",
        "The schema cannot be published because it does not meet the minimum requirements.",
        StatusCodes.Status400BadRequest);

    /// <summary>
    /// Creates a detailed error for missing core fields listing which ones are missing.
    /// </summary>
    public static Error CoreFieldsMissing(IReadOnlyList<string> missingFieldNames)
    {
        var details = string.Join(", ", missingFieldNames);
        return new Error(
            "Intake.CoreFieldMissing",
            $"The following required core fields are missing from the schema: {details}.",
            StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Creates a detailed error for empty required fields in a submission.
    /// </summary>
    public static Error RequiredFieldsEmpty(IReadOnlyList<string> emptyFieldNames)
    {
        var details = string.Join(", ", emptyFieldNames);
        return new Error(
            "Intake.RequiredFieldEmpty",
            $"The following required fields cannot be empty: {details}.",
            StatusCodes.Status400BadRequest);
    }
}