namespace PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;

public record SubmissionGroupDto
{
    public string GroupId { get; init; } = string.Empty;
    public List<SubmissionAnswerDto> Answers { get; init; } = new();
}
