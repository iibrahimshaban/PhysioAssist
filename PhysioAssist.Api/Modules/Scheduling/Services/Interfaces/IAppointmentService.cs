using PhysioAssist.Api.Modules.Scheduling.DTO;
using PhysioAssist.Api.Shared.ResultPattern;

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
        Task<IReadOnlyList<AvailableIntervalDto>> GetAvailabilityAsync(Guid doctorId, DateTimeOffset date, CancellationToken cancellationToken = default);

        /// <summary>
        /// Permanently deletes an appointment record.
        /// Unlike <see cref="CancelAsync"/>, this removes the row entirely and does not
        /// preserve it for history — use Cancel for the normal "patient/doctor called it off" flow.
        /// </summary>
        Task<Result> DeleteAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    }
}