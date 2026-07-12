namespace PhysioAssist.Api.Modules.Intake.DTOs.Submissions;

public record SubmitPreVisitIntakeRequest
{
    public string FormSubmissionData { get; init; } = string.Empty;
    public string? PainPointsData { get; init; }
}
