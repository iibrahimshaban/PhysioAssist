using PhysioAssist.Api.Modules.Scheduling.DTO;
using PhysioAssist.Api.Shared.Dtos.Schedule;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Interfaces
{
    public interface IAppointmentService
    {
        Task<Result<ScheduleSlotDto>> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default);
        Task<Result<ScheduleSlotDto>> CancelAsync(Guid appointmentId, CancellationToken cancellationToken = default);
        Task<Result<ScheduleSlotDto>> RescheduleAsync(Guid appointmentId, RescheduleAppointmentRequest request, CancellationToken cancellationToken = default);
        Task<Result<ScheduleSlotDto>> CompleteAsync(Guid appointmentId, CancellationToken cancellationToken = default);
        Task<Result<ScheduleSlotDto>> MarkNoShowAsync(Guid appointmentId, CancellationToken cancellationToken = default);
        Task<Result<ScheduleSlotDto>> GetByIdAsync(Guid appointmentId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ScheduleSlotDto>> GetDoctorAppointmentsAsync(Guid doctorId, DateTimeOffset date, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AvailableIntervalDto>> GetAvailabilityAsync(
            Guid doctorId, Guid clinicId, DateTimeOffset date, CancellationToken cancellationToken = default);

        Task<Result> DeleteAsync(Guid appointmentId, CancellationToken cancellationToken = default);

        Task<Result<IReadOnlyList<DailyAvailabilityDto>>> GetAvailabilityRangeAsync(
            Guid doctorId,
            Guid clinicId,
            DateTimeOffset? from = null,
            DateTimeOffset? to = null,
            CancellationToken cancellationToken = default);

        Task<Result<IReadOnlyList<ScheduleSlotDto>>> GetCancelledAppointmentsAsync(
            Guid doctorId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ScheduleSlotDto>> GetClinicAppointmentsAsync(
        Guid clinicId, DateTimeOffset date, CancellationToken cancellationToken = default);

        Task<Result<IReadOnlyList<ScheduleSlotDto>>> GetCancelledClinicAppointmentsAsync(
            Guid clinicId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default);
    }
}