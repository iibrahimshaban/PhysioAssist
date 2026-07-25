using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.Repositories.Interfaces;

namespace PhysioAssist.Api.Modules.Scheduling.Repositories.Implementations
{
    public class WorkingScheduleRepository(ApplicationDbContext context)
    : BaseRepository<WorkingSchedule>(context), IWorkingScheduleRepository
    {
        private readonly ApplicationDbContext _context = context;

        public Task<bool> HasActiveScheduleAsync(Guid doctorId, CancellationToken cancellationToken = default) =>
            _context.workingSchedules.AnyAsync(w => w.DoctorId == doctorId && w.IsActive, cancellationToken);

        public Task<WorkingSchedule?> GetActiveScheduleWithDaysAsync(Guid doctorId, CancellationToken cancellationToken = default) =>
            _context.workingSchedules
                .Include(w => w.Days)
                .FirstOrDefaultAsync(w => w.DoctorId == doctorId && w.IsActive, cancellationToken);

        public Task<WorkingSchedule?> GetByIdWithDaysAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.workingSchedules
                .Include(w => w.Days)
                .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }
}
