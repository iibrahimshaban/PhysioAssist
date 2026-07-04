using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.Repositories.Interfaces;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Repositories;

namespace PhysioAssist.Api.Modules.Scheduling.Repositories.Implementations
{
    public class WorkingScheduleDayRepository(ApplicationDbContext context)
    : BaseRepository<WorkingScheduleDay>(context), IWorkingScheduleDayRepository
    {
        public Task<WorkingScheduleDay?> GetWorkingDayAsync(
          Guid doctorId,
          DayOfWeek dayOfWeek,
          CancellationToken cancellationToken = default) =>
          _context.workingScheduleDays
              .Include(d => d.WorkingSchedule)
              .Where(d => d.WorkingSchedule.DoctorId == doctorId &&
                          d.WorkingSchedule.IsActive &&
                          d.Day == dayOfWeek)
              .FirstOrDefaultAsync(cancellationToken);
    }
}
