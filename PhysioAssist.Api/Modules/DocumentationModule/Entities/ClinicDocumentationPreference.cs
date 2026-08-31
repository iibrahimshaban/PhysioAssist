namespace PhysioAssist.Api.Modules.DocumentationModule.Entities;

public class ClinicDocumentationPreference : AuditableEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ClinicId { get; set; }
    public Guid DocumentationTemplateId { get; set; }
    public string? HiddenFieldIds { get; set; }   // JSON array, e.g. ["coordination", "sensation"]
    public DocumentationTemplate Template { get; set; } = default!;   // same module, real navigation
}
