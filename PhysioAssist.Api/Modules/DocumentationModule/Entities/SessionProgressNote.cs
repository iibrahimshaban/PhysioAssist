namespace PhysioAssist.Api.Modules.DocumentationModule.Entities;

public class SessionProgressNote : AuditableEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid SessionId { get; set; }
    public Guid DocumentationTemplateId { get; set; }
    public string Subjective { get; set; } = string.Empty;
    public string? ObjectiveFindings { get; set; }   // JSON, shape driven by DocumentationTemplate
    public string Assessment { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public bool IsDeleted { get; set; } = false;
    public DocumentationTemplate Template { get; set; } = default!;
}
