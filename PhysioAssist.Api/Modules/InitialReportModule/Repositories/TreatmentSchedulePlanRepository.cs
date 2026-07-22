using PhysioAssist.Api.Modules.InitialReportModule.Entities;

namespace PhysioAssist.Api.Modules.InitialReportModule.Repositories;

public class TreatmentSchedulePlanRepository(ApplicationDbContext context) : ITreatmentSchedulePlanRepository
{
    private readonly ApplicationDbContext _context = context;

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
