using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.Repositories.Implementations;
using PhysioAssist.Api.Modules.Scheduling.Repositories.Interfaces;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Interfaces;

namespace PhysioAssist.Api.Shared.Repositories;

public class UnitOfWork: IUnitOfWork
{

    public IScheduleSlotRepository ScheduleSlots { get; }
    public IWorkingScheduleRepository WorkingSchedules { get; }
    public IWorkingScheduleDayRepository WorkingScheduleDays { get; }

    private readonly Dictionary<Type, object> _repositories = [];

    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        ScheduleSlots = new ScheduleSlotRepository(context);
        WorkingSchedules = new WorkingScheduleRepository(context);
        WorkingScheduleDays = new WorkingScheduleDayRepository(context);
    }

    public IBaseRepository<TEntity> Repository<TEntity>()
        where TEntity : class
    {
        var entityType = typeof(TEntity);

        if (!_repositories.TryGetValue(entityType, out var repository))
        {
            repository = new BaseRepository<TEntity>(_context);
            _repositories.Add(entityType, repository);
        }

        return (IBaseRepository<TEntity>)repository;
    }
    public async Task SaveAsync(CancellationToken cancellation) => await _context.SaveChangesAsync(cancellation);

    //public void Dispose()
    //{
    //    throw new NotImplementedException();
    //}
}
