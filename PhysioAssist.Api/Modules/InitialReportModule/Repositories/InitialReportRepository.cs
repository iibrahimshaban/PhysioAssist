using PhysioAssist.Api.Modules.InitialReportModule.Entities;
using PhysioAssist.Api.Shared.Helpers;

namespace PhysioAssist.Api.Modules.InitialReportModule.Repositories;

public class InitialReportRepository(ApplicationDbContext context) : BaseRepository<InitialReport>(context), IInitialReportRepository
{
    public async Task<InitialReport?> GetByIdAsync(Guid id)
    {
        return await _context.InitialReports
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<InitialReport?> GetWithAttachmentsAsync(Guid id)
    {
        return await _context.InitialReports
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<InitialReport>> GetByPatientIdAsync(Guid patientId)
    {
        return await _context.InitialReports
            .Where(r => r.PatientId == patientId)
            .Include(r => r.Attachments)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }
    public async Task<InitialReport?> GetReportByPatientIdAsync(Guid patientId)
    {
        return await _context.InitialReports
            .Include(r => r.Attachments)
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.CreatedAt) 
            .FirstOrDefaultAsync();
    }
    public async Task<string?> GetTreatmentPlanTextAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var reportText = await _context.InitialReports
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => r.ReportText) 
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(reportText))
            return null;

        var (_, treatmentPlan) = ReportTextFormatter.Split(reportText);
        return string.IsNullOrEmpty(treatmentPlan) ? null : treatmentPlan;
    }
}