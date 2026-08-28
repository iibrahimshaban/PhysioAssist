using PhysioAssist.Api.Modules.DashboardModule.Contracts;

namespace PhysioAssist.Api.Modules.DashboardModule.Services;

public interface IDoctorDashboardService
{
    Task<Result<DoctorDashboardSummaryDto>> GetSummaryAsync(
        Guid clinicId,
        string doctorFirstName,
        CancellationToken cancellationToken = default);
}
