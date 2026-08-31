using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.Repositories.Interfaces;

namespace PhysioAssist.Api.Modules.Scheduling.Repositories.Implementations
{
    public class WorkingScheduleRepository(ApplicationDbContext context)
    : BaseRepository<WorkingSchedule>(context), IWorkingScheduleRepository
    {
        public async Task<WorkingScheduleDay?> GetEffectiveWorkingDayAsync(
             Guid doctorId, Guid clinicId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default)
        {
            var overrideDay = await _context.Set<WorkingScheduleDay>()
                .Include(d => d.WorkingSchedule)
                .FirstOrDefaultAsync(d => d.WorkingSchedule.DoctorId == doctorId
                                           && d.WorkingSchedule.IsActive
                                           && d.Day == dayOfWeek, cancellationToken);

            if (overrideDay is not null)
                return overrideDay;

            return await _context.Set<WorkingScheduleDay>()
                .Include(d => d.WorkingSchedule)
                .FirstOrDefaultAsync(d => d.WorkingSchedule.ClinicId == clinicId
                                           && d.WorkingSchedule.DoctorId == null
                                           && d.WorkingSchedule.IsActive
                                           && d.Day == dayOfWeek, cancellationToken);
        }
        public Task<bool> HasActiveOverrideAsync(Guid doctorId, CancellationToken cancellationToken = default) =>
        _context.workingSchedules.AnyAsync(w => w.DoctorId == doctorId && w.IsActive, cancellationToken);

        public Task<WorkingSchedule?> GetActiveOverrideWithDaysAsync(Guid doctorId, CancellationToken cancellationToken = default) =>
            _context.workingSchedules
                .Include(w => w.Days)
                .FirstOrDefaultAsync(w => w.DoctorId == doctorId && w.IsActive, cancellationToken);

        public Task<bool> HasActiveClinicDefaultAsync(Guid clinicId, CancellationToken cancellationToken = default) =>
            _context.workingSchedules.AnyAsync(w => w.ClinicId == clinicId && w.DoctorId == null && w.IsActive, cancellationToken);

        public Task<WorkingSchedule?> GetActiveClinicDefaultWithDaysAsync(Guid clinicId, CancellationToken cancellationToken = default) =>
            _context.workingSchedules
                .Include(w => w.Days)
                .FirstOrDefaultAsync(w => w.ClinicId == clinicId && w.DoctorId == null && w.IsActive, cancellationToken);

        public async Task<WorkingSchedule?> GetEffectiveScheduleWithDaysAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default)
        {
            var doctorOverride = await GetActiveOverrideWithDaysAsync(doctorId, cancellationToken);
            return doctorOverride ?? await GetActiveClinicDefaultWithDaysAsync(clinicId, cancellationToken);
        }

        public Task<WorkingSchedule?> GetByIdWithDaysAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.workingSchedules
                .Include(w => w.Days)
                .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        public Task<List<Guid>> GetDoctorIdsWithActiveOverrideAsync(
            IReadOnlyList<Guid> doctorIds, CancellationToken cancellationToken = default) =>
            _context.workingSchedules
                .Where(w => w.DoctorId != null && doctorIds.Contains(w.DoctorId.Value) && w.IsActive)
                .Select(w => w.DoctorId!.Value)
                .ToListAsync(cancellationToken);
    }
}
