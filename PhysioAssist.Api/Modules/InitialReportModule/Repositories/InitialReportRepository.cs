using PhysioAssist.Api.Modules.InitialReportModule.Entities;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Repositories;

namespace PhysioAssist.Api.Modules.InitialReportModule.Repositories;

public class InitialReportRepository : BaseRepository<InitialReport>, IInitialReportRepository
{
    public InitialReportRepository(ApplicationDbContext context) : base(context)
    {
    }

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
}
