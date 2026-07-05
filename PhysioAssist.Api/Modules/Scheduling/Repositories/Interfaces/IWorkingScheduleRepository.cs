using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Shared.Interfaces;

namespace PhysioAssist.Api.Modules.Scheduling.Repositories.Interfaces
{
    public interface IWorkingScheduleRepository : IBaseRepository<WorkingSchedule>
    {

        Task<bool> HasActiveScheduleAsync(Guid doctorId, CancellationToken cancellationToken = default);

       
        Task<WorkingSchedule?> GetActiveScheduleWithDaysAsync(Guid doctorId, CancellationToken cancellationToken = default);

        Task<WorkingSchedule?> GetByIdWithDaysAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
