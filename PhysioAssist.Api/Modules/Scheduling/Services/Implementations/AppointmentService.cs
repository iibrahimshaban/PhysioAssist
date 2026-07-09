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

    private async Task<Result<ScheduleSlot>> GetOrNotFoundAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var appointment = await _unitOfWork.ScheduleSlots.GetByIdAsync(appointmentId, cancellationToken);

        return appointment is null
            ? Result.Failure<ScheduleSlot>(AppointmentErrors.NotFound(appointmentId))
            : Result.Success(appointment);
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