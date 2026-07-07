using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Shared.Interfaces;

namespace PhysioAssist.Api.Modules.Scheduling.Repositories.Interfaces
{
    public interface IScheduleSlotRepository : IBaseRepository<ScheduleSlot>
    {
        Task<ScheduleSlot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> HasOverlapAsync(
          Guid doctorId,
          DateTime slotStart,
          DateTime slotEnd,
          Guid? excludeAppointmentId = null,
          CancellationToken cancellationToken = default);

        Task<List<ScheduleSlot>> GetDoctorAppointmentsForDayAsync(
            Guid doctorId,
            DateTime date,
            CancellationToken cancellationToken = default);

        Task<List<ScheduleSlot>> GetDoctorAppointmentsAsync(
            Guid doctorId,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);
    }
}
