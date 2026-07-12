namespace PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;

public record FormGroupDto
{
    public string GroupId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int Order { get; init; }
    public List<FormQuestionDto> Questions { get; init; } = new();
}
