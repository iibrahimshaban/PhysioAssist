namespace PhysioAssist.Api.Modules.Intake.DTOs.PublicAccess;

public record PublicIntakeFormResponse
{
    public Guid FormSchemaId { get; init; }
    public string FormName { get; init; } = string.Empty;
    public string? FormDescription { get; init; }
    public string SchemaJson { get; init; } = string.Empty;
    public bool ShowPainMap { get; init; }
    public int Version { get; init; }
}
