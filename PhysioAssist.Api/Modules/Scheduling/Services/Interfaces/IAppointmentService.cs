using PhysioAssist.Api.Modules.Scheduling.DTO;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Interfaces
{
    public interface IAppointmentService
    {

        Task<ScheduleSlotDto> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default);
        Task<ScheduleSlotDto> CancelAsync(Guid appointmentId, CancellationToken cancellationToken = default);
        Task<ScheduleSlotDto> RescheduleAsync(Guid appointmentId, RescheduleAppointmentRequest request, CancellationToken cancellationToken = default);
        Task<ScheduleSlotDto> CompleteAsync(Guid appointmentId, CancellationToken cancellationToken = default);
        Task<ScheduleSlotDto> MarkNoShowAsync(Guid appointmentId, CancellationToken cancellationToken = default);
        Task<ScheduleSlotDto?> GetByIdAsync(Guid appointmentId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ScheduleSlotDto>> GetDoctorAppointmentsAsync(Guid doctorId, DateTime date, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AvailableIntervalDto>> GetAvailabilityAsync(Guid doctorId, DateTime date, CancellationToken cancellationToken = default);
    }
}
