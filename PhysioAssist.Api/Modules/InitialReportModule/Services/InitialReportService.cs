using Microsoft.AspNetCore.Identity;
using PhysioAssist.Api.Modules.Auth.Entities;
using PhysioAssist.Api.Modules.InitialReportModule.DTOs;
using PhysioAssist.Api.Modules.InitialReportModule.Entities;
using PhysioAssist.Api.Modules.InitialReportModule.Errors;
using PhysioAssist.Api.Modules.InitialReportModule.Repositories;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Dtos.Pdf;
using PhysioAssist.Api.Shared.Dtos.Transcription;
using PhysioAssist.Api.Shared.Interfaces;
using PhysioAssist.Api.Shared.SystemPrompts;

namespace PhysioAssist.Api.Modules.InitialReportModule.Services;

// ⚠️ الـ Patient/Doctor lookup هنا بيتعمل مباشرة عن طريق ApplicationDbContext وUserManager
// لأن IPatientQueryService مش موجودة فعليًا في الكود، وDoctorId بيتفترض إنه = ApplicationUser.Id.
// لو الافتراضين دول غلط، قوليلي عشان نظبطهم.
public class InitialReportService(
    IInitialReportRepository reportRepository,
    IReportAttachmentRepository attachmentRepository,
    IAudioTranscriptionService audioTranscriptionService,
    IMediaStorageService mediaStorageService,
    IPdfService pdfService,
    IQRService qrService,
    INotificationService notificationService,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext context,
    IUnitOfWork unitOfWork) : IInitialReportService
{
    private readonly IInitialReportRepository _reportRepository = reportRepository;
    private readonly IReportAttachmentRepository _attachmentRepository = attachmentRepository;
    private readonly IAudioTranscriptionService _audioTranscriptionService = audioTranscriptionService;
    private readonly IMediaStorageService _mediaStorageService = mediaStorageService;
    private readonly IPdfService _pdfService = pdfService;
    private readonly IQRService _qrService = qrService;
    private readonly INotificationService _notificationService = notificationService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ApplicationDbContext _context = context;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<InitialReportResponse>> CreateAsync(Guid doctorId, CreateInitialReportRequest request)
    {
        var report = new InitialReport
        {
            Id = Guid.CreateVersion7(),
            DoctorId = doctorId,
            PatientId = request.PatientId,
            ReportText = request.ReportText ?? string.Empty
        };

        await _reportRepository.AddAsync(report);
        await _unitOfWork.SaveAsync();

        return Result.Success(MapToResponse(report));
    }

    public async Task<Result<InitialReportResponse>> GetByIdAsync(Guid reportId)
    {
        var report = await _reportRepository.GetWithAttachmentsAsync(reportId);

        return report is null
            ? Result.Failure<InitialReportResponse>(InitialReportErrors.NotFound)
            : Result.Success(MapToResponse(report));
    }

    public async Task<Result<InitialReportResponse>> UpdateReportTextAsync(Guid reportId, UpdateReportTextRequest request)
    {
        var report = await _reportRepository.GetWithAttachmentsAsync(reportId);

        if (report is null)
            return Result.Failure<InitialReportResponse>(InitialReportErrors.NotFound);

        report.ReportText = request.ReportText;
        _reportRepository.Update(report);
        await _unitOfWork.SaveAsync();

        return Result.Success(MapToResponse(report));
    }

    public async Task<Result<InitialReportResponse>> TranscribeAsync(Guid reportId, IFormFile audioFile, string? languageHint)
    {
        var report = await _reportRepository.GetWithAttachmentsAsync(reportId);

        if (report is null)
            return Result.Failure<InitialReportResponse>(InitialReportErrors.NotFound);

        await using var audioStream = audioFile.OpenReadStream();

        var transcriptionRequest = new TranscriptionRequest(
            audioStream,
            audioFile.FileName,
            languageHint,
            TranscriptionPrompts.GeminiInitialReportTranscription);

        var transcriptionResult = await _audioTranscriptionService.TranscribeAsync(transcriptionRequest);

        if (transcriptionResult.IsFailure)
            return Result.Failure<InitialReportResponse>(InitialReportErrors.TranscriptionFailed);

        report.ReportText = transcriptionResult.Value.RefinedText;
        _reportRepository.Update(report);
        await _unitOfWork.SaveAsync();

        return Result.Success(MapToResponse(report));
    }

    public async Task<Result<ReportAttachmentResponse>> UploadAttachmentAsync(Guid reportId, IFormFile file)
    {
        var report = await _reportRepository.GetByIdAsync(reportId);

        if (report is null)
            return Result.Failure<ReportAttachmentResponse>(InitialReportErrors.NotFound);

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return Result.Failure<ReportAttachmentResponse>(InitialReportErrors.InvalidFileType);

        var publicId = Guid.CreateVersion7().ToString();
        var folder = $"initial-reports/{reportId}";

        string fileUrl;
        try
        {
            fileUrl = await _mediaStorageService.UploadImageAsync(file, folder, publicId);
        }
        catch
        {
            return Result.Failure<ReportAttachmentResponse>(InitialReportErrors.AttachmentUploadFailed);
        }

        var attachment = new ReportAttachment
        {
            Id = Guid.CreateVersion7(),
            ReportId = reportId,
            FileUrl = fileUrl,
            FileType = file.ContentType,
            FileName = file.FileName
        };

        await _attachmentRepository.AddAsync(attachment);
        await _unitOfWork.SaveAsync();

        return Result.Success(new ReportAttachmentResponse(
            attachment.Id, attachment.FileUrl, attachment.FileType, attachment.FileName));
    }

    public async Task<Result> DeleteAttachmentAsync(Guid reportId, Guid attachmentId)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(attachmentId);

        if (attachment is null || attachment.ReportId != reportId)
            return Result.Failure(InitialReportErrors.AttachmentNotFound);

        try
        {
            await _mediaStorageService.DeleteImageByUrlAsync(attachment.FileUrl);
        }
        catch
        {
            // Best-effort cleanup on remote storage; proceed with local deletion regardless.
        }

        _attachmentRepository.Delete(attachment);
        await _unitOfWork.SaveAsync();

        return Result.Success();
    }

    public async Task<Result<InitialReportResponse>> SubmitAsync(Guid reportId)
    {
        var report = await _reportRepository.GetWithAttachmentsAsync(reportId);

        if (report is null)
            return Result.Failure<InitialReportResponse>(InitialReportErrors.NotFound);

        var patient = await _context.Patients.FindAsync(report.PatientId);

        if (patient is null)
            return Result.Failure<InitialReportResponse>(InitialReportErrors.PatientNotFound);

        var doctor = await _userManager.FindByIdAsync(report.DoctorId.ToString());
        var doctorFullName = doctor is null ? "Doctor" : $"{doctor.FirstName} {doctor.LastName}";

        // 1. Generate treatment plan PDF
        var pdfRequest = new TreatmentPlanPdfRequest(
            report.Id, patient.FullName, doctorFullName, report.ReportText, DateTime.UtcNow);

        var pdfUrl = await _pdfService.GenerateTreatmentPlanPdfAsync(pdfRequest);
        report.TreatmentPlanPdfUrl = pdfUrl;

        // 2. Generate signed QR code pointing to the patient
        var qrToken = _qrService.GenerateSignedToken(patient.Id, "patient-report-access");
        var qrImageUrl = await _qrService.GenerateQrImageUrlAsync(
            qrToken, $"qr-codes/{patient.Id}", $"report-{report.Id}");

        _reportRepository.Update(report);
        await _unitOfWork.SaveAsync();

        // 3. Dispatch email notification (WhatsApp deferred)
        await _notificationService.SendReportReadyNotificationAsync(
            report.DoctorId, patient.Id, patient.EmailAddress, patient.FullName, pdfUrl, qrImageUrl);

        return Result.Success(MapToResponse(report));
    }

    private static InitialReportResponse MapToResponse(InitialReport report) => new(
        report.Id,
        report.DoctorId,
        report.PatientId,
        report.ReportText,
        report.TreatmentPlanPdfUrl,
        report.CreatedAt,
        report.Attachments
            .Select(a => new ReportAttachmentResponse(a.Id, a.FileUrl, a.FileType, a.FileName))
            .ToList());
}
