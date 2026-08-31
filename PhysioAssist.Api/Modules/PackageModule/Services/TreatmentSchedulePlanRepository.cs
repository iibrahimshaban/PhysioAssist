using PhysioAssist.Api.Modules.PackageModule.Entities;

namespace PhysioAssist.Api.Modules.PackageModule.Services;

public class TreatmentSchedulePlanRepository(ApplicationDbContext context) : BaseRepository<TreatmentSchedulePlan>(context), ITreatmentSchedulePlanRepository
{
    public async Task<TreatmentSchedulePlan?> GetByReportIdAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TreatmentSchedulePlan>()
            .FirstOrDefaultAsync(p => p.ReportId == reportId, cancellationToken);
    }

    public async Task AddAsync(TreatmentSchedulePlan plan, CancellationToken cancellationToken = default)
    {
        await _context.Set<TreatmentSchedulePlan>().AddAsync(plan, cancellationToken);
    }

    public void Update(TreatmentSchedulePlan plan)
    {
        _context.Set<TreatmentSchedulePlan>().Update(plan);
    }
}
