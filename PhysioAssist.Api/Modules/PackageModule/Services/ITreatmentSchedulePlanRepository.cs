using PhysioAssist.Api.Modules.PackageModule.Entities;

namespace PhysioAssist.Api.Modules.PackageModule.Services;

public interface ITreatmentSchedulePlanRepository :IBaseRepository<TreatmentSchedulePlan>
{
    Task<TreatmentSchedulePlan?> GetByReportIdAsync(Guid reportId, CancellationToken cancellationToken = default);
    Task AddAsync(TreatmentSchedulePlan plan, CancellationToken cancellationToken = default);
    void Update(TreatmentSchedulePlan plan);
}
