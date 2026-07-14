namespace PhysioAssist.Api.Modules.Intake.DTOs.FormSchemas;

public record FormSchemaSummaryResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Version { get; init; }
    public FormSchemaStatus Status { get; init; }
    public bool IsDefault { get; init; }
    public DateTime? PublishedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
