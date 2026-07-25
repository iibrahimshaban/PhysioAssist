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
        Task<IReadOnlyList<AvailableIntervalDto>> GetAvailabilityAsync(Guid doctorId, DateTimeOffset date, CancellationToken cancellationToken = default);

        /// <summary>
        /// Permanently deletes an appointment record.
        /// Unlike <see cref="CancelAsync"/>, this removes the row entirely and does not
        /// preserve it for history — use Cancel for the normal "patient/doctor called it off" flow.
        /// </summary>
        Task<Result> DeleteAsync(Guid appointmentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates the doctor's free (bookable) time intervals for every working day
        /// within [from, to]. If both are omitted, defaults to the current calendar week.
        /// Fails if the doctor has no active WorkingSchedule; non-working days inside the
        /// range are simply skipped (not an error), same as the single-day endpoint.
        /// </summary>
        Task<Result<IReadOnlyList<DailyAvailabilityDto>>> GetAvailabilityRangeAsync(
            Guid doctorId,
            DateTimeOffset? from = null,
            DateTimeOffset? to = null,
            CancellationToken cancellationToken = default);

        Task<Result<IReadOnlyList<ScheduleSlotDto>>> GetCancelledAppointmentsAsync(
            Guid doctorId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            CancellationToken cancellationToken = default);
    }
}