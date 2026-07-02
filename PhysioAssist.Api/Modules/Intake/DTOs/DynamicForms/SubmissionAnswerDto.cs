namespace PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;

public record SubmissionAnswerDto
{
    public string QuestionId { get; init; } = string.Empty;
    public Dictionary<string, object>? Value { get; init; }
    public string? Notes { get; init; }
    public List<AttachmentAnswerDto>? Attachments { get; init; }
}
