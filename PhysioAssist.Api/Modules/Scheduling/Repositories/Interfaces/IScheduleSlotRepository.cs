using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Shared.Interfaces.Common;

namespace PhysioAssist.Api.Modules.Scheduling.Repositories.Interfaces
{
    public interface IScheduleSlotRepository : IBaseRepository<ScheduleSlot>
    {
        Task<ScheduleSlot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> HasOverlapAsync(
          Guid doctorId,
          DateTimeOffset slotStart,
          DateTimeOffset slotEnd,
          Guid? excludeAppointmentId = null,
          CancellationToken cancellationToken = default);

        Task<List<ScheduleSlot>> GetDoctorAppointmentsForDayAsync(
            Guid doctorId,
            DateTimeOffset date,
            CancellationToken cancellationToken = default);

        Task<List<ScheduleSlot>> GetDoctorAppointmentsAsync(
            Guid doctorId,
            DateTimeOffset from,
            DateTimeOffset to,
            CancellationToken cancellationToken = default);

        Task<List<ScheduleSlot>> GetCancelledAppointmentsAsync(
            Guid doctorId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            CancellationToken cancellationToken = default);

        Task<List<ScheduleSlot>> GetBookedAppointmentsAsync(
            Guid doctorId,
            CancellationToken cancellationToken = default);

        
        Task<List<ScheduleSlot>> GetFutureBookedAppointmentsAsync(
            Guid doctorId,
            DateTimeOffset from,
            CancellationToken cancellationToken = default);
    }
}
