using PhysioAssist.Api.Modules.InitialReportModule.DTOs;

namespace PhysioAssist.Api.Modules.InitialReportModule.Services;

public interface IInitialReportService
{
    Task<Result<InitialReportResponse>> CreateAsync(Guid doctorId, CreateInitialReportRequest request);
    Task<Result<InitialReportResponse>> GetByIdAsync(Guid reportId);
    Task<Result<InitialReportResponse>> UpdateReportTextAsync(Guid reportId, UpdateReportTextRequest request);
    Task<Result<InitialReportResponse>> TranscribeAsync(Guid reportId, IFormFile audioFile, string? languageHint);
    Task<Result<ReportAttachmentResponse>> UploadAttachmentAsync(Guid reportId, IFormFile file);
    Task<Result> DeleteAttachmentAsync(Guid reportId, Guid attachmentId);
<<<<<<< HEAD

    /// <summary>
    /// Generates the treatment plan PDF, generates a QR code, and dispatches an email
    /// notification to the patient with both.
    /// </summary>
    Task<Result<InitialReportResponse>> SubmitAsync(Guid reportId);
=======
    Task<Result<InitialReportResponse>> GetByPatientIdAsync(Guid patientId);
>>>>>>> be94d86bf95f3c039134e9161e18565aa145bc99
}
