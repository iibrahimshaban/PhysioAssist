using PhysioAssist.Api.Modules.Scheduling.DTO;
using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.Errors;
using PhysioAssist.Api.Modules.Scheduling.helpers;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;
using PhysioAssist.Api.Shared.Interfaces;
using PhysioAssist.Api.Shared.ResultPattern;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Implementations;

public class AppointmentService(
    IUnitOfWork unitOfWork,
    IAppointmentValidator validator)
    : IAppointmentService
{
    // Maximum span (inclusive) allowed for an explicit from/to availability-range request.
    private const int MaxRangeDays = 31;

    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAppointmentValidator _validator = validator;

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

        // Cancel-old + book-new — preserves an honest audit trail instead of
        // silently rewriting the original appointment's times.
        existing.Status = SlotStatus.Cancelled;

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
            return new List<AvailableIntervalDto>(); // doctor doesn't work this day — no availability, not an error

        var appointments = await _unitOfWork.ScheduleSlots.GetDoctorAppointmentsForDayAsync(doctorId, date, cancellationToken);

        return AvailabilityCalculator.CalculateFreeIntervals(DateOnly.FromDateTime(date.Date), workingDay, appointments);
    }

    public async Task<Result> DeleteAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
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

        // Reuse the existing repository — don't duplicate the "doctor working?" check
        // per day. A missing active schedule is a hard failure here (unlike the
        // single-day endpoint, which returns an empty list for a non-working day).
        var schedule = await _unitOfWork.WorkingSchedules.GetActiveScheduleWithDaysAsync(doctorId, cancellationToken);
        if (schedule is null)
            return Result.Failure<IReadOnlyList<DailyAvailabilityDto>>(WorkingScheduleErrors.NoActiveScheduleFound(doctorId));

        var workingDaysByWeekday = schedule.Days.ToDictionary(d => d.Day);

        // Single range query instead of one query per day.
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
            // Non-working weekdays inside the range are skipped, same philosophy
            // as GetAvailabilityAsync (no working day => no entry, not an error).

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
        // Only validate a range if one was actually provided — unlike availability,
        // "no range" is a valid, meaningful request here (return every cancelled
        // appointment for this doctor), not something that needs a default window.
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

    /// <summary>
    /// Resolves the effective [from, to] window for a range-availability request.
    /// If both are omitted, defaults to the current calendar week.
    /// ASSUMPTION: week is Sunday–Saturday — no existing week-start convention was
    /// found elsewhere in the project. Adjust here if that's wrong.
    /// ASSUMPTION: from/to must be supplied together; a single one without the other
    /// is rejected rather than silently defaulted.
    /// </summary>
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