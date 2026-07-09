namespace PhysioAssist.Api.Modules.DocumentationModule.Entities;

public class DocumentationSummary : AuditableEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid DoctorId { get; set; }
    public Guid PatientId { get; set; }
    public SummaryAudience Audience { get; set; }
    public SummaryScope? Scope { get; set; }              // null when Audience = Patient
    public string? FocusAreas { get; set; }               // JSON array, only when Scope = Focused
    public bool AnonymizePersonalData { get; set; }
    public string SummaryText { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;   // Cloudinary — same pattern as TreatmentPlanPdfUrl
    public bool IsDeleted { get; set; } = false;
}
