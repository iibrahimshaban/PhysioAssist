namespace PhysioAssist.Api.Modules.DashboardModule.Contracts;

public record PendingIntakePreviewDto
{
    public Guid SubmissionId { get; init; }
    public string PatientFullName { get; init; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; init; }
    public int PainRegionsCount { get; init; }
}
