namespace PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;

public record DynamicFormSchemaDto
{
    public int SchemaVersion { get; init; }
    public List<FormSectionDto> Sections { get; init; } = new();
}
