namespace PhysioAssist.Api.Modules.DocumentationModule.Errors;

public static class DocumentationErrors
{
    public static readonly Error TemplateNotFound = new(
        "Documentation.TemplateNotFound",
        "The requested documentation template was not found.",StatusCodes.Status404NotFound);

    public static readonly Error InvalidSchemaJson = new(
        "Documentation.InvalidSchemaJson",
        "The stored template schema could not be parsed.",StatusCodes.Status400BadRequest);

    public static readonly Error EmptyTranscript = new(
       "Documentation.EmptyTranscript",
       "Cannot extract objective findings from an empty transcript.",
       StatusCodes.Status400BadRequest);

    public static readonly Error NoFieldsToExtract = new(
        "Documentation.NoFieldsToExtract",
        "No effective fields were provided to extract against.",
        StatusCodes.Status400BadRequest);

    public static readonly Error ExtractionFailed = new(
        "Documentation.ExtractionFailed",
        "The AI model did not return a valid response for objective findings extraction.",
        StatusCodes.Status502BadGateway);

    public static readonly Error TranscriptNotFound = new(
        "Documentation.TranscriptNotFound",
        "No finalized transcript was found for this session.",
        StatusCodes.Status404NotFound);

    public static readonly Error ProgressNoteNotFound = new(
        "Documentation.ProgressNoteNotFound",
        "No progress note was found for this session.",
        StatusCodes.Status404NotFound);

    public static readonly Error CategoryNotSet = new(
        "Documentation.CategoryNotSet",
        "This doctor-patient relationship has no PatientCategory set, so no documentation template applies.",
        StatusCodes.Status400BadRequest);

    public static readonly Error SummaryGenerationFailed = new(
        "Documentation.SummaryGenerationFailed",
        "The AI model did not return a valid session summary.",
        StatusCodes.Status502BadGateway);

    public static readonly Error SessionNotFound = new(
        "Documentation.SessionNotFound",
        "The session could not be found when saving the generated summary.",
        StatusCodes.Status404NotFound);

    public static readonly Error NoSessionsFound = new(
        "Documentation.NoSessionsFound",
        "No sessions with a generated summary were found for this patient.",
        StatusCodes.Status404NotFound);

    public static readonly Error RollupGenerationFailed = new(
        "Documentation.RollupGenerationFailed",
        "The AI model did not return a valid case summary.",
        StatusCodes.Status502BadGateway);

    public static readonly Error DocumentationSummaryNotFound = new(
        "Documentation.DocumentationSummaryNotFound",
        "The requested documentation summary was not found.",
        StatusCodes.Status404NotFound);
}
