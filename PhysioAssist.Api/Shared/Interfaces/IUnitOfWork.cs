using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.Repositories.Interfaces;

namespace PhysioAssist.Api.Shared.Interfaces;

public interface IUnitOfWork 
{
    public IBaseRepository<TEntity> Repository<TEntity>() where TEntity : class;

    IScheduleSlotRepository ScheduleSlots { get; }

    IWorkingScheduleRepository WorkingSchedules { get; }

    IWorkingScheduleDayRepository WorkingScheduleDays { get; }

    Task SaveAsync(CancellationToken cancellationToken= default);
}
