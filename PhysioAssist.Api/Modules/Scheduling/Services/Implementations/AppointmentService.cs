using PhysioAssist.Api.Modules.Scheduling.DTO;
using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.Errors;
using PhysioAssist.Api.Modules.Scheduling.helpers;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;
using PhysioAssist.Api.Modules.Notification.DTO;
using PhysioAssist.Api.Modules.Notification.Interfaces;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Implementations;

public class AppointmentService(
    IUnitOfWork unitOfWork,
    IAppointmentValidator validator,
    INotificationService notificationService,
    IAppointmentContactResolver contactResolver)
    : IAppointmentService
{
    private const int MaxRangeDays = 31;

    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAppointmentValidator _validator = validator;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IAppointmentContactResolver _contactResolver = contactResolver;

    public async Task<Result<ScheduleSlotDto>> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateCreateAsync(request, cancellationToken);
        if (validation.IsFailure)
            return Result.Failure<ScheduleSlotDto>(validation.Error);

        var appointment = new ScheduleSlot
        {
            Id = Guid.CreateVersion7(),
            DoctorId = request.DoctorId,
            PatientId = request.PatientId,
            SlotStart = request.SlotStart,
            SlotEnd = request.SlotEnd,
            Status = SlotStatus.Booked
        };

        await _unitOfWork.ScheduleSlots.AddAsync(appointment);
        await _unitOfWork.SaveAsync(cancellationToken);

        await NotifyAsync(appointment, _notificationService.NotifyAppointmentCreatedAsync, cancellationToken);

        return Result.Success(MapToDto(appointment));
    }

    public async Task<Result<ScheduleSlotDto>> CancelAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var lookup = await GetOrNotFoundAsync(appointmentId, cancellationToken);
        if (lookup.IsFailure)
            return Result.Failure<ScheduleSlotDto>(lookup.Error);

        var appointment = lookup.Value;

        var validation = _validator.ValidateCancel(appointment);
        if (validation.IsFailure)
            return Result.Failure<ScheduleSlotDto>(validation.Error);

        appointment.Status = SlotStatus.Cancelled;

        await _unitOfWork.SaveAsync(cancellationToken);

        await NotifyAsync(appointment, _notificationService.NotifyAppointmentCancelledAsync, cancellationToken);

        return Result.Success(MapToDto(appointment));
    }

    public async Task<Result<ScheduleSlotDto>> RescheduleAsync(Guid appointmentId, RescheduleAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        var lookup = await GetOrNotFoundAsync(appointmentId, cancellationToken);
        if (lookup.IsFailure)
            return Result.Failure<ScheduleSlotDto>(lookup.Error);

        var existing = lookup.Value;

        var validation = await _validator.ValidateRescheduleAsync(existing, request, cancellationToken);
        if (validation.IsFailure)
            return Result.Failure<ScheduleSlotDto>(validation.Error);

        
        var result = await DeleteAsync(existing.Id, cancellationToken);
        if (result.IsSuccess)
        {

            var replacement = new ScheduleSlot
            {
                Id = Guid.CreateVersion7(),
                DoctorId = existing.DoctorId,
                PatientId = existing.PatientId,
                SlotStart = request.NewSlotStart,
                SlotEnd = request.NewSlotEnd,
                Status = SlotStatus.Booked
            };

            await _unitOfWork.ScheduleSlots.AddAsync(replacement);
            await _unitOfWork.SaveAsync(cancellationToken);

            await NotifyAsync(replacement, _notificationService.NotifyAppointmentRescheduledAsync, cancellationToken);

            return Result.Success(MapToDto(replacement));
        }
        else
        {
            return Result.Failure<ScheduleSlotDto>(result.Error);
        }
      
    }

    public async Task<Result<ScheduleSlotDto>> CompleteAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var lookup = await GetOrNotFoundAsync(appointmentId, cancellationToken);
        if (lookup.IsFailure)
            return Result.Failure<ScheduleSlotDto>(lookup.Error);

        var appointment = lookup.Value;

        var validation = _validator.ValidateComplete(appointment);
        if (validation.IsFailure)
            return Result.Failure<ScheduleSlotDto>(validation.Error);

        appointment.Status = SlotStatus.Completed;

        await _unitOfWork.SaveAsync(cancellationToken);

        await NotifyAsync(appointment, _notificationService.NotifyAppointmentCompletedAsync, cancellationToken);

        return Result.Success(MapToDto(appointment));
    }

    public async Task<Result<ScheduleSlotDto>> MarkNoShowAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var lookup = await GetOrNotFoundAsync(appointmentId, cancellationToken);
        if (lookup.IsFailure)
            return Result.Failure<ScheduleSlotDto>(lookup.Error);

        var appointment = lookup.Value;

        var validation = _validator.ValidateNoShow(appointment);
        if (validation.IsFailure)
            return Result.Failure<ScheduleSlotDto>(validation.Error);

        appointment.Status = SlotStatus.NoShow;

        await _unitOfWork.SaveAsync(cancellationToken);

        await NotifyAsync(appointment, _notificationService.NotifyAppointmentNoShowAsync, cancellationToken);

        return Result.Success(MapToDto(appointment));
    }

    public async Task<Result<ScheduleSlotDto>> GetByIdAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await _unitOfWork.ScheduleSlots.GetByIdAsync(appointmentId, cancellationToken);

        return appointment is null
            ? Result.Failure<ScheduleSlotDto>(AppointmentErrors.NotFound(appointmentId))
            : Result.Success(MapToDto(appointment));
    }

    public async Task<IReadOnlyList<ScheduleSlotDto>> GetDoctorAppointmentsAsync(Guid doctorId, DateTimeOffset date, CancellationToken cancellationToken = default)
    {
        var appointments = await _unitOfWork.ScheduleSlots.GetDoctorAppointmentsForDayAsync(doctorId, date, cancellationToken);
        return appointments.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<AvailableIntervalDto>> GetAvailabilityAsync(Guid doctorId, DateTimeOffset date, CancellationToken cancellationToken = default)
    {
        var workingDay = await _unitOfWork.WorkingScheduleDays.GetWorkingDayAsync(doctorId, date.DayOfWeek, cancellationToken);

        if (workingDay is null)
            return new List<AvailableIntervalDto>();

        var appointments = await _unitOfWork.ScheduleSlots.GetDoctorAppointmentsForDayAsync(doctorId, date, cancellationToken);

        return AvailabilityCalculator.CalculateFreeIntervals(DateOnly.FromDateTime(date.Date), workingDay, appointments);
    }

    public async Task<Result> DeleteAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        // NOTE: no notification is sent here by design. A hard delete means the
        // record is being erased outright (e.g. created by mistake) — per your
        // own controller docs, this is NOT the "appointment called off" flow
        // (that's Cancel), so there's no meaningful "your appointment was
        // deleted" message to send a patient about something that, from their
        // perspective, should simply cease to have existed.
        var lookup = await GetOrNotFoundAsync(appointmentId, cancellationToken);
        if (lookup.IsFailure)
            return Result.Failure(lookup.Error);

        _unitOfWork.ScheduleSlots.Delete(lookup.Value);
        await _unitOfWork.SaveAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<DailyAvailabilityDto>>> GetAvailabilityRangeAsync(
        Guid doctorId,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var rangeResult = ResolveRange(from, to);
        if (rangeResult.IsFailure)
            return Result.Failure<IReadOnlyList<DailyAvailabilityDto>>(rangeResult.Error);

        var (rangeStart, rangeEnd) = rangeResult.Value;

        var schedule = await _unitOfWork.WorkingSchedules.GetActiveScheduleWithDaysAsync(doctorId, cancellationToken);
        if (schedule is null)
            return Result.Failure<IReadOnlyList<DailyAvailabilityDto>>(WorkingScheduleErrors.NoActiveScheduleFound(doctorId));

        var workingDaysByWeekday = schedule.Days.ToDictionary(d => d.Day);

        var appointments = await _unitOfWork.ScheduleSlots.GetDoctorAppointmentsAsync(doctorId, rangeStart, rangeEnd, cancellationToken);

        var appointmentsByDate = appointments
            .GroupBy(a => DateOnly.FromDateTime(a.SlotStart.Date))
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<DailyAvailabilityDto>();
        var currentDate = DateOnly.FromDateTime(rangeStart.Date);
        var lastDate = DateOnly.FromDateTime(rangeEnd.Date);

        while (currentDate <= lastDate)
        {
            if (workingDaysByWeekday.TryGetValue(currentDate.DayOfWeek, out var workingDay))
            {
                appointmentsByDate.TryGetValue(currentDate, out var dayAppointments);

                var intervals = AvailabilityCalculator.CalculateFreeIntervals(
                    currentDate,
                    workingDay,
                    dayAppointments ?? []);

                result.Add(new DailyAvailabilityDto
                {
                    Date = currentDate,
                    Intervals = intervals
                });
            }

            currentDate = currentDate.AddDays(1);
        }

        return Result.Success<IReadOnlyList<DailyAvailabilityDto>>(result);
    }

    public async Task<Result<IReadOnlyList<ScheduleSlotDto>>> GetCancelledAppointmentsAsync(
        Guid doctorId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default)
    {
        if (from.HasValue || to.HasValue)
        {
            if (from.HasValue != to.HasValue)
                return Result.Failure<IReadOnlyList<ScheduleSlotDto>>(AppointmentErrors.RangeIncomplete);

            if (to!.Value.Date < from!.Value.Date)
                return Result.Failure<IReadOnlyList<ScheduleSlotDto>>(AppointmentErrors.RangeEndBeforeStart);
        }

        var cancelled = await _unitOfWork.ScheduleSlots.GetCancelledAppointmentsAsync(doctorId, from, to, cancellationToken);

        var dtos = cancelled.Select(MapToDto).ToList();

        return Result.Success<IReadOnlyList<ScheduleSlotDto>>(dtos);
    }

    private async Task<Result<ScheduleSlot>> GetOrNotFoundAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var appointment = await _unitOfWork.ScheduleSlots.GetByIdAsync(appointmentId, cancellationToken);

        return appointment is null
            ? Result.Failure<ScheduleSlot>(AppointmentErrors.NotFound(appointmentId))
            : Result.Success(appointment);
    }

    private static Result<(DateTimeOffset Start, DateTimeOffset End)> ResolveRange(DateTimeOffset? from, DateTimeOffset? to)
    {
        if (from is null && to is null)
        {
            var today = DateTimeOffset.UtcNow;
            var weekStart = today.Date.AddDays(-(int)today.DayOfWeek);
            var weekEnd = weekStart.AddDays(6);

            return Result.Success<(DateTimeOffset, DateTimeOffset)>((
                new DateTimeOffset(weekStart, TimeSpan.Zero),
                new DateTimeOffset(weekEnd, TimeSpan.Zero)));
        }

        if (from is null || to is null)
            return Result.Failure<(DateTimeOffset, DateTimeOffset)>(AppointmentErrors.RangeIncomplete);

        if (to.Value.Date < from.Value.Date)
            return Result.Failure<(DateTimeOffset, DateTimeOffset)>(AppointmentErrors.RangeEndBeforeStart);

        var totalDays = (to.Value.Date - from.Value.Date).TotalDays + 1;
        if (totalDays > MaxRangeDays)
            return Result.Failure<(DateTimeOffset, DateTimeOffset)>(AppointmentErrors.RangeTooLarge(MaxRangeDays));

        return Result.Success<(DateTimeOffset, DateTimeOffset)>((from.Value, to.Value));
    }

    /// <summary>
    /// Resolves patient/doctor display info for the given appointment and invokes
    /// the matching notification method. Any failure — contact lookup failing,
    /// or the notification call itself throwing — is swallowed here, never
    /// propagated to the caller. By the time this runs, the appointment's own
    /// database change has already been saved successfully; a notification
    /// problem must never retroactively affect that already-completed operation.
    /// </summary>
    private async Task NotifyAsync(
        ScheduleSlot appointment,
        Func<AppointmentNotificationDto, CancellationToken, Task> notify,
        CancellationToken cancellationToken)
    {
        try
        {
            var (patientName, patientEmail) = await _contactResolver.GetPatientContactAsync(appointment.PatientId, cancellationToken);
            var doctorName = await _contactResolver.GetDoctorNameAsync(appointment.DoctorId, cancellationToken);

            var dto = new AppointmentNotificationDto
            {
                PatientEmail = patientEmail,
                PatientName = patientName,
                DoctorName = doctorName,
                SlotStart = appointment.SlotStart,
                SlotEnd = appointment.SlotEnd
            };

            await notify(dto, cancellationToken);
        }
        catch
        {
            // Swallowed by design — see method summary. NotificationService
            // already logs its own internal delivery failures; this catch only
            // guards the CONTACT LOOKUP step itself failing (e.g. Patient/Doctor
            // module unreachable), which NotificationService never sees since
            // it's never invoked in that case.
        }
    }

    private static ScheduleSlotDto MapToDto(ScheduleSlot slot) => new()
    {
        Id = slot.Id,
        DoctorId = slot.DoctorId,
        PatientId = slot.PatientId,
        SlotStart = slot.SlotStart,
        SlotEnd = slot.SlotEnd,
        Status = slot.Status.ToString()
    };
}
