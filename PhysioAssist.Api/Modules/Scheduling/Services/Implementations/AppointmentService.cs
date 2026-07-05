using PhysioAssist.Api.Modules.Scheduling.DTO;
using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.helpers;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;
using PhysioAssist.Api.Shared.Interfaces;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Implementations;

public class AppointmentService(
    IUnitOfWork unitOfWork,
    IAppointmentValidator validator)
    : IAppointmentService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAppointmentValidator _validator = validator;

    public async Task<ScheduleSlotDto> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateCreateAsync(request, cancellationToken);

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

        return MapToDto(appointment);
    }

    public async Task<ScheduleSlotDto> CancelAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await GetOrThrowAsync(appointmentId, cancellationToken);

        _validator.ValidateCancel(appointment);

        appointment.Status = SlotStatus.Cancelled;

        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToDto(appointment);
    }

    public async Task<ScheduleSlotDto> RescheduleAsync(Guid appointmentId, RescheduleAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await GetOrThrowAsync(appointmentId, cancellationToken);

        await _validator.ValidateRescheduleAsync(existing, request, cancellationToken);

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

        return MapToDto(replacement);
    }

    public async Task<ScheduleSlotDto> CompleteAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await GetOrThrowAsync(appointmentId, cancellationToken);

        _validator.ValidateComplete(appointment);

        appointment.Status = SlotStatus.Completed;

        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToDto(appointment);
    }

    public async Task<ScheduleSlotDto> MarkNoShowAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await GetOrThrowAsync(appointmentId, cancellationToken);

        _validator.ValidateNoShow(appointment);

        appointment.Status = SlotStatus.NoShow;

        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToDto(appointment);
    }

    public async Task<ScheduleSlotDto?> GetByIdAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await _unitOfWork.ScheduleSlots.GetByIdAsync(appointmentId, cancellationToken);
        return appointment is null ? null : MapToDto(appointment);
    }

    public async Task<IReadOnlyList<ScheduleSlotDto>> GetDoctorAppointmentsAsync(Guid doctorId, DateTime date, CancellationToken cancellationToken = default)
    {
        var appointments = await _unitOfWork.ScheduleSlots.GetDoctorAppointmentsForDayAsync(doctorId, date, cancellationToken);
        return appointments.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<AvailableIntervalDto>> GetAvailabilityAsync(Guid doctorId, DateTime date, CancellationToken cancellationToken = default)
    {
        var workingDay = await _unitOfWork.WorkingScheduleDays.GetWorkingDayAsync(doctorId, date.DayOfWeek, cancellationToken);

        if (workingDay is null)
            return new List<AvailableIntervalDto>(); // doctor doesn't work this day — no availability, not an error

        var appointments = await _unitOfWork.ScheduleSlots.GetDoctorAppointmentsForDayAsync(doctorId,  date, cancellationToken);

        return AvailabilityCalculator.CalculateFreeIntervals(DateOnly.FromDateTime(date), workingDay, appointments);
    }

    private async Task<ScheduleSlot> GetOrThrowAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var appointment = await _unitOfWork.ScheduleSlots.GetByIdAsync(appointmentId, cancellationToken);

        if (appointment is null)
            throw new SchedulingNotFoundException($"Appointment {appointmentId} was not found.");

        return appointment;
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