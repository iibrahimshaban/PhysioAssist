namespace PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;

public record SubmissionAnswerDto
{
    public string QuestionId { get; init; } = string.Empty;
    // Accept any JSON value for the answer (primitive, object, array).
    // System.Text.Json will map this to a JsonElement at runtime when deserializing.
    public object? Value { get; init; }
    public string? Notes { get; init; }
    public List<AttachmentAnswerDto>? Attachments { get; init; }
}
