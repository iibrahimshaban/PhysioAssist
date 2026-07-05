using PhysioAssist.Api.Modules.InitialReportModule.Entities;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Repositories;

namespace PhysioAssist.Api.Modules.InitialReportModule.Repositories;

public class ReportAttachmentRepository : BaseRepository<ReportAttachment>, IReportAttachmentRepository
{
    public ReportAttachmentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<ReportAttachment?> GetByIdAsync(Guid id)
    {
        return await _context.ReportAttachments
            .FirstOrDefaultAsync(a => a.Id == id);
    }
}
