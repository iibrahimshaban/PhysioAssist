namespace PhysioAssist.Api.Modules.InitialReportModule.DTOs;

public record CreateInitialReportRequest(Guid PatientId, string? ReportText);

public record UpdateReportTextRequest(string ReportText);

public record ReportAttachmentResponse(
    Guid Id,
    string FileUrl,
    string FileType,
    string FileName);

public record InitialReportResponse(
    Guid Id,
    Guid DoctorId,
    Guid PatientId,
    string ReportText,
    string? TreatmentPlanPdfUrl,
    DateTime CreatedAt,
    List<ReportAttachmentResponse> Attachments);
