namespace PhysioAssist.Api.Modules.Intake.Entities;

public class PatientFormSchema : AuditableEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SchemaJson { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public int Version { get; set; } = 1;
    public FormSchemaStatus Status { get; set; } = FormSchemaStatus.Draft;
    public bool IsDefault { get; set; }
    public string SchemaHash { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
}
