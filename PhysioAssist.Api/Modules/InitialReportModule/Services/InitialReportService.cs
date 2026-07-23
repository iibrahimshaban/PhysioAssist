using Microsoft.Extensions.Options;
using PhysioAssist.Api.Infrastructure.GeminiClient;
using PhysioAssist.Api.Modules.InitialReportModule.DTOs;
using PhysioAssist.Api.Modules.InitialReportModule.Entities;
using PhysioAssist.Api.Modules.InitialReportModule.Errors;
using PhysioAssist.Api.Modules.InitialReportModule.Repositories;
using PhysioAssist.Api.Shared.Dtos.Pdf;
using PhysioAssist.Api.Shared.Dtos.Transcription;
using PhysioAssist.Api.Shared.Interfaces.Documentation;
using PhysioAssist.Api.Shared.Options;

namespace PhysioAssist.Api.Modules.InitialReportModule.Services;

public class InitialReportService(
    IInitialReportRepository reportRepository,
    IReportAttachmentRepository attachmentRepository,
    IAudioTranscriptionService audioTranscriptionService,
    IMediaStorageService mediaStorageService,
    IPdfService pdfService,
    IQRService qrService,
    INotificationService notificationService,
    IPatientQueryService _patientQueryService,
    IUnitOfWork unitOfWork,
    IAuthQueryService _authQueryService,
    IOptions<FrontendSettings> _frontendSettings,
    IPatientSummaryAiService _patientSummaryAiService,
    IPatientSessionSchedulingService _PatientSessionSchedulingService
) : IInitialReportService
{
    private readonly IInitialReportRepository _reportRepository = reportRepository;
    private readonly IReportAttachmentRepository _attachmentRepository = attachmentRepository;
    private readonly IAudioTranscriptionService _audioTranscriptionService = audioTranscriptionService;
    private readonly IMediaStorageService _mediaStorageService = mediaStorageService;
    private readonly IPdfService _pdfService = pdfService;
    private readonly IQRService _qrService = qrService;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    private const string PdfContentType = "application/pdf";

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

        var isImage = file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        var isPdf = file.ContentType.Equals(PdfContentType, StringComparison.OrdinalIgnoreCase);

        if (!isImage && !isPdf)
            return Result.Failure<ReportAttachmentResponse>(InitialReportErrors.InvalidFileType);

        var publicId = Guid.CreateVersion7().ToString();
        var folder = $"initial-reports/{reportId}";

        string fileUrl;
        try
        {
            if (isPdf)
            {
                await using var stream = file.OpenReadStream();
                var extension = Path.GetExtension(file.FileName).TrimStart('.');
                if (string.IsNullOrEmpty(extension))
                    extension = "pdf";

                fileUrl = await _mediaStorageService.UploadRawFileAsync(stream, folder, publicId, extension);
            }
            else
            {
                fileUrl = await _mediaStorageService.UploadClinicalImageAsync(file, folder, publicId);
            }
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
    public async Task<Result<InitialReportResponse>> GetByPatientIdAsync(Guid patientId)
    {
        var report = await _reportRepository.GetReportByPatientIdAsync(patientId);

        return report is null
            ? Result.Failure<InitialReportResponse>(InitialReportErrors.NotFound)
            : Result.Success(MapToResponse(report));
    }
    public async Task<Result<InitialReportResponse>> SubmitAsync(Guid reportId)
    {
        var report = await _reportRepository.GetWithAttachmentsAsync(reportId);

        if (report is null)
            return Result.Failure<InitialReportResponse>(InitialReportErrors.NotFound);

        var patient = await _patientQueryService.GetPatientAsync(report.PatientId);

        if (patient is null)
            return Result.Failure<InitialReportResponse>(InitialReportErrors.PatientNotFound);

        var doctor = await _authQueryService.GetDoctorById(report.DoctorId);
        var doctorFullName = doctor.IsFailure ? "Doctor" : $"{doctor.Value.FirstName} {doctor.Value.LastName}";

        var summaryResult = await _patientSummaryAiService.GeneratePatientFriendlySummaryAsync(report.ReportText);

        if (summaryResult.IsFailure)
            return Result.Failure<InitialReportResponse>(summaryResult.Error);

        // 1.  Generate QR pointing to the patient profile
        var qrUrl = $"{_frontendSettings.Value.BaseUrl}/app/patients/{patient.Value.Id}";
        var qrBytes = _qrService.GenerateQrImageBytes(qrUrl);

        // 1b. Look up the patient's first booked session, if any, to print on the PDF.
        // Null-safe: if the patient somehow has no booked session yet, the section is
        // simply skipped rather than failing the whole report submission over it.
        var firstSession = await _PatientSessionSchedulingService.GetFirstBookedSessionForPatientAsync(patient.Value.Id);

        // 2. Generate treatment plan PDF, QR as its own section
        var summaryParagraphs = summaryResult.Value
            .Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var sections = new List<PdfSection>
    {
        new(null,
        [
            $"Patient: {patient.Value.FullName}",
            $"Doctor: {doctorFullName}",
            $"Date: {DateTime.UtcNow:yyyy-MM-dd}"
        ]),
        new("Your Summary", summaryParagraphs)
    };

        if (firstSession is not null)
        {
            sections.Add(new PdfSection("Your First Appointment",
            [
                $"{firstSession.SlotStart:dddd, MMMM d, yyyy} at {firstSession.SlotStart:h:mm tt}"
            ]));
        }

        sections.Add(new PdfSection("Scan to access your profile", [], qrBytes));

        var pdfContent = new PdfDocumentContent(
                    Title: "PhysioAssist — Treatment Plan",
                    Sections: sections);

        var pdfResult = await _pdfService.GeneratePdfAsync(
            pdfContent, $"treatment-plans/{report.Id}", $"treatment-plan-{report.Id}");

        if (pdfResult.IsFailure)
            return Result.Failure<InitialReportResponse>(pdfResult.Error);

        report.TreatmentPlanPdfUrl = pdfResult.Value;

        _reportRepository.Update(report);
        await _unitOfWork.SaveAsync();

        // 3. Dispatch email with PDF attached — background job, WhatsApp deferred
        await _notificationService.SendReportReadyNotificationAsync(
            report.DoctorId, patient.Value.Id, patient.Value.EmailAddress, patient.Value.FullName, report.TreatmentPlanPdfUrl);

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