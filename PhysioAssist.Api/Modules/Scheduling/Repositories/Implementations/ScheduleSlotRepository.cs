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
            Guid doctorId,
            DateTimeOffset date,
            CancellationToken cancellationToken = default)
        {
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            return await _context.Set<ScheduleSlot>()
                .Where(x =>
                    x.DoctorId == doctorId &&
                    x.SlotStart >= dayStart &&
                    x.SlotStart < dayEnd)
                .OrderBy(x => x.SlotStart)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ScheduleSlot>> GetDoctorAppointmentsAsync(
            Guid doctorId,
            DateTimeOffset from,
            DateTimeOffset to,
            CancellationToken cancellationToken = default)
        {
            return await _context.Set<ScheduleSlot>()
                .Where(x =>
                    x.DoctorId == doctorId &&
                    x.SlotStart >= from &&
                    x.SlotEnd <= to)
                .OrderBy(x => x.SlotStart)
                .ToListAsync(cancellationToken);
        }

        // Repositories/Implementations/ScheduleSlotRepository.cs — add this method to the existing class
        public Task<List<ScheduleSlot>> GetCancelledAppointmentsAsync(
            Guid doctorId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            CancellationToken cancellationToken = default)
        {
            var query = _context.ScheduleSlots
                .Where(s => s.DoctorId == doctorId && s.Status == SlotStatus.Cancelled);

            if (from.HasValue)
                query = query.Where(s => s.SlotStart >= from.Value);

            if (to.HasValue)
                query = query.Where(s => s.SlotStart <= to.Value);

            // Most recently cancelled first — matches how a receptionist would want
            // to review cancellations (newest first), unlike appointment-day queries
            // which order chronologically ascending.
            return query.OrderByDescending(s => s.SlotStart).ToListAsync(cancellationToken);
        }


        public Task<List<ScheduleSlot>> GetBookedAppointmentsAsync(
            Guid doctorId,
            CancellationToken cancellationToken = default)
        {
            return _context.ScheduleSlots
                .Where(s => s.DoctorId == doctorId && s.Status == SlotStatus.Booked )
                .ToListAsync(cancellationToken);
        }

        
        public Task<List<ScheduleSlot>> GetFutureBookedAppointmentsAsync(
            Guid doctorId,
            DateTimeOffset from,
            CancellationToken cancellationToken = default)
        {
            return _context.ScheduleSlots
                .Where(s => s.DoctorId == doctorId && s.Status == SlotStatus.Booked && s.SlotStart >= from)
                .ToListAsync(cancellationToken);
        }
    }
}
