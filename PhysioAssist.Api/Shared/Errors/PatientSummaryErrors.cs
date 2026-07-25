namespace PhysioAssist.Api.Shared.Errors;

public static class PatientSummaryErrors
{
    public static readonly Error EmptyInput = new(
        "PatientSummary.EmptyInput", "Report text is empty.", StatusCodes.Status400BadRequest);

    public static readonly Error GenerationFailed = new(
        "PatientSummary.GenerationFailed", "Failed to generate a patient-friendly summary.", StatusCodes.Status500InternalServerError);
}
