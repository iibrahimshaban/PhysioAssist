namespace PhysioAssist.Api.Modules.Intake.DTOs.PublicAccess;

public record PublicIntakeSubmissionResponse
{
    public Guid SubmissionId { get; init; }
    public string ShortCode { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime SubmittedAt { get; init; }
}
