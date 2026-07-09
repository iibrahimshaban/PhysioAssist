using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Shared.Interfaces;

namespace PhysioAssist.Api.Modules.Scheduling.Repositories.Interfaces
{
    public interface  IWorkingScheduleDayRepository
            : IBaseRepository<WorkingScheduleDay>
            {
                Task<WorkingScheduleDay?> GetWorkingDayAsync(
                Guid doctorId,
                DayOfWeek day,
                CancellationToken cancellationToken = default);
            }
}
