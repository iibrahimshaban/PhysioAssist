using PhysioAssist.Api.Modules.Scheduling.DTO;
using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.Errors;
using PhysioAssist.Api.Modules.Scheduling.Repositories.Interfaces;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;
using PhysioAssist.Api.Shared.ResultPattern;

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

        public async Task<Result> ValidateCreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default)
        {
            var shapeResult = ValidateBasicShape(request.SlotStart, request.SlotEnd);
            if (shapeResult.IsFailure)
                return shapeResult;

            var workingHoursResult = await ValidateWorkingHoursAsync(request.DoctorId, request.SlotStart, request.SlotEnd, cancellationToken);
            if (workingHoursResult.IsFailure)
                return workingHoursResult;

            return await ValidateOverlapAsync(request.DoctorId, request.SlotStart, request.SlotEnd, excludeAppointmentId: null, cancellationToken);
        }

        public async Task<Result> ValidateRescheduleAsync(ScheduleSlot existing, RescheduleAppointmentRequest request, CancellationToken cancellationToken = default)
        {
            if (existing.Status != SlotStatus.Booked)
                return Result.Failure(AppointmentErrors.InvalidStatusForReschedule(existing.Status));

            var shapeResult = ValidateBasicShape(request.NewSlotStart, request.NewSlotEnd);
            if (shapeResult.IsFailure)
                return shapeResult;

            var workingHoursResult = await ValidateWorkingHoursAsync(existing.DoctorId, request.NewSlotStart, request.NewSlotEnd, cancellationToken);
            if (workingHoursResult.IsFailure)
                return workingHoursResult;

            // Exclude the appointment being moved — otherwise it would "overlap itself"
            return await ValidateOverlapAsync(existing.DoctorId, request.NewSlotStart, request.NewSlotEnd, existing.Id, cancellationToken);
        }

        public Result ValidateCancel(ScheduleSlot existing)
        {
            return existing.Status != SlotStatus.Booked
                ? Result.Failure(AppointmentErrors.InvalidStatusForCancel(existing.Status))
                : Result.Success();
        }

        public Result ValidateComplete(ScheduleSlot existing)
        {
            return existing.Status != SlotStatus.Booked
                ? Result.Failure(AppointmentErrors.InvalidStatusForComplete(existing.Status))
                : Result.Success();
        }

        public Result ValidateNoShow(ScheduleSlot existing)
        {
            return existing.Status != SlotStatus.Booked
                ? Result.Failure(AppointmentErrors.InvalidStatusForNoShow(existing.Status))
                : Result.Success();
        }

        private static Result ValidateBasicShape(DateTimeOffset slotStart, DateTimeOffset slotEnd)
        {
            if (slotEnd <= slotStart)
                return Result.Failure(AppointmentErrors.EndBeforeStart);

            var duration = slotEnd - slotStart;

            if (duration.TotalMinutes < MinDurationMinutes)
                return Result.Failure(AppointmentErrors.DurationTooShort(MinDurationMinutes));

            if (duration.TotalMinutes > MaxDurationMinutes)
                return Result.Failure(AppointmentErrors.DurationTooLong(MaxDurationMinutes));

            if (slotStart.Date != slotEnd.Date)
                return Result.Failure(AppointmentErrors.SpansMultipleDays);

            return Result.Success();
        }

        private async Task<Result> ValidateWorkingHoursAsync(Guid doctorId, DateTimeOffset slotStart, DateTimeOffset slotEnd, CancellationToken cancellationToken)
        {
            var workingDay = await _workingScheduleDayRepository.GetWorkingDayAsync(doctorId, slotStart.DayOfWeek, cancellationToken);

            if (workingDay is null)
                return Result.Failure(AppointmentErrors.DoctorNotWorking(slotStart.DayOfWeek));

            var startTime = TimeOnly.FromTimeSpan(slotStart.TimeOfDay);
            var endTime = TimeOnly.FromTimeSpan(slotEnd.TimeOfDay);

            if (startTime < workingDay.StartTime || endTime > workingDay.EndTime)
                return Result.Failure(AppointmentErrors.OutsideWorkingHours(workingDay.StartTime, workingDay.EndTime));

            return Result.Success();
        }

        private async Task<Result> ValidateOverlapAsync(Guid doctorId, DateTimeOffset slotStart, DateTimeOffset slotEnd, Guid? excludeAppointmentId, CancellationToken cancellationToken)
        {
            var hasOverlap = await _scheduleSlotRepository.HasOverlapAsync(doctorId, slotStart, slotEnd, excludeAppointmentId, cancellationToken);

            return hasOverlap ? Result.Failure(AppointmentErrors.Overlap) : Result.Success();
        }
    }
}