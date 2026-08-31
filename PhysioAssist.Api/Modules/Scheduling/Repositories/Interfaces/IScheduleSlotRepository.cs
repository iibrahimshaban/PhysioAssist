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
            IReadOnlyList<Guid> doctorIds,
            DateTimeOffset date,
            CancellationToken cancellationToken = default);

        Task<List<ScheduleSlot>> GetDoctorAppointmentsAsync(
            IReadOnlyList<Guid> doctorIds,
            DateTimeOffset from,
            DateTimeOffset to,
            CancellationToken cancellationToken = default);

        Task<List<ScheduleSlot>> GetCancelledAppointmentsAsync(
            IReadOnlyList<Guid> doctorIds,
            DateTimeOffset? from,
            DateTimeOffset? to,
            CancellationToken cancellationToken = default);

        Task<List<ScheduleSlot>> GetBookedAppointmentsAsync(
            IReadOnlyList<Guid> doctorIds,
            CancellationToken cancellationToken = default);

        Task<List<ScheduleSlot>> GetFutureBookedAppointmentsAsync(
            IReadOnlyList<Guid> doctorIds,
            DateTimeOffset from,
            CancellationToken cancellationToken = default);
    }
}
