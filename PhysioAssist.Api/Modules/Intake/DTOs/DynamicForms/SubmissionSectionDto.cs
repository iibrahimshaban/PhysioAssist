namespace PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;

public record SubmissionSectionDto
{
    public string SectionId { get; init; } = string.Empty;
    public List<SubmissionGroupDto> Groups { get; init; } = new();
}
