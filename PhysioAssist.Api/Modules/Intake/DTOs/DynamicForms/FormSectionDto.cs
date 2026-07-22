namespace PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;

public record FormSectionDto
{
    public string SectionId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int Order { get; init; }
    public List<FormGroupDto> Groups { get; init; } = new();
}
