namespace PhysioAssist.Api.Modules.Intake.DTOs.Submissions;

public record SubmitPreVisitIntakeRequest
{
    public string PatientName { get; init; } = string.Empty;
    public string? PatientEmail { get; init; }
    public string? PatientPhone { get; init; }
    public string FormSubmissionData { get; init; } = string.Empty;
    public string? PainPointsData { get; init; }
}
