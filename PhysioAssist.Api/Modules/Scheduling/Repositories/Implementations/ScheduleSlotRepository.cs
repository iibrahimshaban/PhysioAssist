using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.Repositories.Interfaces;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Repositories;

namespace PhysioAssist.Api.Modules.Scheduling.Repositories.Implementations
{
    public class ScheduleSlotRepository(ApplicationDbContext context)
    : BaseRepository<ScheduleSlot>(context), IScheduleSlotRepository
    {

        public Task<ScheduleSlot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
       _context.ScheduleSlots.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        public Task<bool> HasOverlapAsync(
        Guid doctorId,
        DateTime slotStart,
        DateTime slotEnd,
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
            DateTime date,
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
            DateTime from,
            DateTime to,
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
    }
}
