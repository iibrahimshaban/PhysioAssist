using PhysioAssist.Api.Modules.InitialReportModule.DTOs;
using PhysioAssist.Api.Modules.InitialReportModule.Repositories;

namespace PhysioAssist.Api.Modules.InitialReportModule.Services;

public class InitialReportQueryService(IInitialReportRepository reportRepository) : IInitialReportQueryService
{
    private readonly IInitialReportRepository _reportRepository = reportRepository;

    public async Task<List<InitialReportResponse>> GetPatientReportsAsync(Guid patientId)
    {
        var reports = await _reportRepository.GetByPatientIdAsync(patientId);
        return reports.Select(MapToResponse).ToList();
    }

    public async Task<InitialReportResponse?> GetReportWithAttachmentsAsync(Guid reportId)
    {
        var report = await _reportRepository.GetWithAttachmentsAsync(reportId);
        return report is null ? null : MapToResponse(report);
    }

    private static InitialReportResponse MapToResponse(Entities.InitialReport report) => new(
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
