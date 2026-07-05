using PhysioAssist.Api.Modules.Scheduling.DTO;
using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.helpers;
using PhysioAssist.Api.Modules.Scheduling.Repositories.Interfaces;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Implementations
{
    public class AppointmentValidator(IScheduleSlotRepository scheduleSlotRepository,
    IWorkingScheduleDayRepository workingScheduleDayRepository)
    : IAppointmentValidator
    {
        private const int MinDurationMinutes = 15;
        private const int MaxDurationMinutes = 240;

        private readonly IScheduleSlotRepository _scheduleSlotRepository = scheduleSlotRepository;
        private readonly IWorkingScheduleDayRepository _workingScheduleDayRepository = workingScheduleDayRepository;

        public async Task ValidateCreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default)
        {
            ValidateBasicShape(request.SlotStart, request.SlotEnd);

            await ValidateWorkingHoursAsync(request.DoctorId, request.SlotStart, request.SlotEnd, cancellationToken);

            await ValidateOverlapAsync(request.DoctorId, request.SlotStart, request.SlotEnd, excludeAppointmentId: null, cancellationToken);
        }

        public async Task ValidateRescheduleAsync(ScheduleSlot existing, RescheduleAppointmentRequest request, CancellationToken cancellationToken = default)
        {
            if (existing.Status != SlotStatus.Booked)
                throw new SchedulingConflictException($"Cannot reschedule an appointment in status {existing.Status}.");

            ValidateBasicShape(request.NewSlotStart, request.NewSlotEnd);

            await ValidateWorkingHoursAsync(existing.DoctorId, request.NewSlotStart, request.NewSlotEnd, cancellationToken);

            // Exclude the appointment being moved — otherwise it would "overlap itself"
            await ValidateOverlapAsync(existing.DoctorId, request.NewSlotStart, request.NewSlotEnd, existing.Id, cancellationToken);
        }

        public void ValidateCancel(ScheduleSlot existing)
        {
            if (existing.Status != SlotStatus.Booked)
                throw new SchedulingConflictException($"Cannot cancel an appointment in status {existing.Status}.");
        }

        public void ValidateComplete(ScheduleSlot existing)
        {
            if (existing.Status != SlotStatus.Booked)
                throw new SchedulingConflictException($"Cannot complete an appointment in status {existing.Status}.");
        }

        public void ValidateNoShow(ScheduleSlot existing)
        {
            if (existing.Status != SlotStatus.Booked)
                throw new SchedulingConflictException($"Cannot mark no-show on an appointment in status {existing.Status}.");
        }

        private static void ValidateBasicShape(DateTime slotStart, DateTime slotEnd)
        {
            if (slotEnd <= slotStart)
                throw new ArgumentException("SlotEnd must be after SlotStart.");

            var duration = slotEnd - slotStart;

            if (duration.TotalMinutes < MinDurationMinutes)
                throw new ArgumentException($"Appointment must be at least {MinDurationMinutes} minutes.");

            if (duration.TotalMinutes > MaxDurationMinutes)
                throw new ArgumentException($"Appointment cannot exceed {MaxDurationMinutes} minutes.");

            if (slotStart.Date != slotEnd.Date)
                throw new ArgumentException("Appointment cannot span multiple days.");
        }

        private async Task ValidateWorkingHoursAsync(Guid doctorId, DateTime slotStart, DateTime slotEnd, CancellationToken cancellationToken)
        {
            var workingDay = await _workingScheduleDayRepository.GetWorkingDayAsync(doctorId, slotStart.DayOfWeek, cancellationToken);

            if (workingDay is null)
                throw new SchedulingConflictException($"Doctor is not working on {slotStart.DayOfWeek}.");

            var startTime = TimeOnly.FromDateTime(slotStart);
            var endTime = TimeOnly.FromDateTime(slotEnd);

            if (startTime < workingDay.StartTime || endTime > workingDay.EndTime)
                throw new SchedulingConflictException(
                    $"Appointment must be within working hours ({workingDay.StartTime} - {workingDay.EndTime}).");
        }

        private async Task ValidateOverlapAsync(Guid doctorId, DateTime slotStart, DateTime slotEnd, Guid? excludeAppointmentId, CancellationToken cancellationToken)
        {
            var hasOverlap = await _scheduleSlotRepository.HasOverlapAsync(doctorId, slotStart, slotEnd, excludeAppointmentId, cancellationToken);

            if (hasOverlap)
                throw new SchedulingConflictException("This appointment overlaps an existing appointment.");
        }
    }
}
