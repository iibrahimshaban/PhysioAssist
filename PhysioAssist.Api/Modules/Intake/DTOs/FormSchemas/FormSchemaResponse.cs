namespace PhysioAssist.Api.Modules.Intake.DTOs.FormSchemas;

public record FormSchemaResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string SchemaJson { get; init; } = string.Empty;
    public Guid DoctorId { get; init; }
    public int Version { get; init; }
    public FormSchemaStatus Status { get; init; }
    public bool IsDefault { get; init; }
    public string SchemaHash { get; init; } = string.Empty;
    public bool ShowPainMap { get; init; } 
    public DateTime? PublishedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string ShortCode { get; init; } = string.Empty;
    public Guid? OriginalFormId { get; init; }
    public string? OriginalName { get; init; }
    public int? CopyNumber { get; init; }
}
