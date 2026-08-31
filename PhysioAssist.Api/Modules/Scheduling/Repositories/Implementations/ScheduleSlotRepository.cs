using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.Repositories.Interfaces;

namespace PhysioAssist.Api.Modules.Scheduling.Repositories.Implementations
{
    public class ScheduleSlotRepository(ApplicationDbContext context)
    : BaseRepository<ScheduleSlot>(context), IScheduleSlotRepository
    {
        public Task<ScheduleSlot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.ScheduleSlots.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        public Task<bool> HasOverlapAsync(
            Guid doctorId,
            DateTimeOffset slotStart,
            DateTimeOffset slotEnd,
            Guid? excludeAppointmentId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.ScheduleSlots.Where(s =>
                s.DoctorId == doctorId &&
                (s.Status == SlotStatus.Booked || s.Status == SlotStatus.Completed) &&
                s.SlotStart < slotEnd &&
                s.SlotEnd > slotStart);

            if (excludeAppointmentId is not null)
                query = query.Where(s => s.Id != excludeAppointmentId);

            return query.AnyAsync(cancellationToken);
        }

        public async Task<List<ScheduleSlot>> GetDoctorAppointmentsForDayAsync(
            IReadOnlyList<Guid> doctorIds,
            DateTimeOffset date,
            CancellationToken cancellationToken = default)
        {
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            return await _context.Set<ScheduleSlot>()
                .Where(x =>
                    doctorIds.Contains(x.DoctorId) &&
                    x.SlotStart >= dayStart &&
                    x.SlotStart < dayEnd)
                .OrderBy(x => x.SlotStart)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ScheduleSlot>> GetDoctorAppointmentsAsync(
            IReadOnlyList<Guid> doctorIds,
            DateTimeOffset from,
            DateTimeOffset to,
            CancellationToken cancellationToken = default)
        {
            return await _context.Set<ScheduleSlot>()
                .Where(x =>
                    doctorIds.Contains(x.DoctorId) &&
                    x.SlotStart >= from &&
                    x.SlotEnd <= to)
                .OrderBy(x => x.SlotStart)
                .ToListAsync(cancellationToken);
        }

        public Task<List<ScheduleSlot>> GetCancelledAppointmentsAsync(
            IReadOnlyList<Guid> doctorIds,
            DateTimeOffset? from,
            DateTimeOffset? to,
            CancellationToken cancellationToken = default)
        {
            var query = _context.ScheduleSlots
                .Where(s => doctorIds.Contains(s.DoctorId) && s.Status == SlotStatus.Cancelled);

            if (from.HasValue)
                query = query.Where(s => s.SlotStart >= from.Value);

            if (to.HasValue)
                query = query.Where(s => s.SlotStart <= to.Value);

            return query.OrderByDescending(s => s.SlotStart).ToListAsync(cancellationToken);
        }

        public Task<List<ScheduleSlot>> GetBookedAppointmentsAsync(
            IReadOnlyList<Guid> doctorIds,
            CancellationToken cancellationToken = default)
        {
            return _context.ScheduleSlots
                .Where(s => doctorIds.Contains(s.DoctorId) && s.Status == SlotStatus.Booked)
                .ToListAsync(cancellationToken);
        }

        public Task<List<ScheduleSlot>> GetFutureBookedAppointmentsAsync(
            IReadOnlyList<Guid> doctorIds,
            DateTimeOffset from,
            CancellationToken cancellationToken = default)
        {
            return _context.ScheduleSlots
                .Where(s => doctorIds.Contains(s.DoctorId) && s.Status == SlotStatus.Booked && s.SlotStart >= from)
                .ToListAsync(cancellationToken);
        }
    }
}