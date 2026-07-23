using PhysioAssist.Api.Modules.InitialReportModule.Entities;

namespace PhysioAssist.Api.Modules.InitialReportModule.Repositories;

public interface ITreatmentSchedulePlanRepository :IBaseRepository<TreatmentSchedulePlan>
{
    Task<TreatmentSchedulePlan?> GetByReportIdAsync(Guid reportId, CancellationToken cancellationToken = default);
    Task AddAsync(TreatmentSchedulePlan plan, CancellationToken cancellationToken = default);
    void Update(TreatmentSchedulePlan plan);
}
