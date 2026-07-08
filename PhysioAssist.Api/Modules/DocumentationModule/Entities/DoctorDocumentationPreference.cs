namespace PhysioAssist.Api.Modules.DocumentationModule.Entities;

public class DoctorDocumentationPreference : AuditableEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid DoctorId { get; set; }
    public Guid DocumentationTemplateId { get; set; }
    public string? HiddenFieldIds { get; set; }   // JSON array, e.g. ["coordination", "sensation"]
    public DocumentationTemplate Template { get; set; } = default!;   // same module, real navigation
}
