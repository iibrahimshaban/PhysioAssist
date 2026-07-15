namespace PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;

public record DynamicFormSubmissionDto
{
    public int SchemaVersion { get; init; }
    public Guid FormSchemaId { get; init; }
    public int FormSchemaVersion { get; init; }
    public List<SubmissionSectionDto> Sections { get; init; } = new();
}
