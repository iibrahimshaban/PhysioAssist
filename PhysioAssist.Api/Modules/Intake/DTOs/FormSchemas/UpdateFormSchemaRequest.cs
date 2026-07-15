namespace PhysioAssist.Api.Modules.Intake.DTOs.FormSchemas;

public record UpdateFormSchemaRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string SchemaJson { get; init; } = string.Empty;
    public bool ShowPainMap { get; init; } = true;
    public bool IsDefault { get; init; }
}
