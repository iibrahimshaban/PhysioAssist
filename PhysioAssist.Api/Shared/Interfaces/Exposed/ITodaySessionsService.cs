using PhysioAssist.Api.Modules.Scheduling.DTO.TodaySessions;

namespace PhysioAssist.Api.Shared.Interfaces.Exposed;

public interface ITodaySessionsService
{
    Task<Result<TodaySessionsOverviewDto>> GetTodaySessionsAsync(Guid doctorId, CancellationToken cancellationToken = default);
}
