namespace PhysioAssist.Api.Modules.Intake.DTOs.PublicAccess;

public record PublicIntakeSubmissionResponse
{
    public Guid SubmissionId { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime SubmittedAt { get; init; }
}
