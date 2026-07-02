namespace PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;

public record AttachmentAnswerDto
{
    public string FileName { get; init; } = string.Empty;
    public string FileUrl { get; init; } = string.Empty;
    public string FileType { get; init; } = string.Empty;
    public long FileSize { get; init; }
}
