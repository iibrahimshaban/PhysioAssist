using PhysioAssist.Api.Modules.Scheduling.DTO.TodaySessions;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;

public interface ITodaySessionsService
{
    Task<Result<TodaySessionsOverviewDto>> GetTodaySessionsAsync(Guid doctorId, CancellationToken cancellationToken = default);
}
