namespace PhysioAssist.Api.Modules.InitialReportModule.Entities;

public class InitialReport : AuditableEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string ReportText { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public Guid PatientId { get; set; }
    public string? TreatmentPlanPdfUrl { get; set; }
    public bool IsDeleted { get; set; } = false;
    public ICollection<ReportAttachment> Attachments { get; set; } = [];
}
