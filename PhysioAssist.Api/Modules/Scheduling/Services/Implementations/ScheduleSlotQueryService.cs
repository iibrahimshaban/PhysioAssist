using PhysioAssist.Api.Modules.Scheduling.DTO;
using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.Errors;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;
using PhysioAssist.Api.Shared.Dtos.Schedule;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Implementations;

public class ScheduleSlotQueryService(ApplicationDbContext context, IAppointmentService appointmentService) : IScheduleSlotQueryService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IAppointmentService _appointmentService = appointmentService;

    public async Task<List<ScheduleSlotResult>> GetUpcomingSlotsForDoctorAsync(Guid doctorId, CancellationToken ct = default)
    {
        return await _context.ScheduleSlots
            .Where(s =>
                s.DoctorId == doctorId &&
                s.SlotStart >= DateTimeOffset.UtcNow &&
                s.Status == SlotStatus.Booked)
            .OrderBy(s => s.SlotStart)
            .Select(s => new ScheduleSlotResult(s.PatientId, s.SlotStart, s.SlotEnd))
            .ToListAsync(ct);
    }
    

    public async Task<Result<PatientSessionPackageDto>> CreatePackageWithFirstBookingAsync(CreatePackageWithFirstBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.TotalSessions <= 0)
            return Result.Failure<PatientSessionPackageDto>(PatientSessionPackageErrors.InvalidTotalSessions);

        // Package and first booking must succeed or fail together — a package with
        // zero real bookings behind it shouldn't be able to exist, per how this was
        // designed (package only comes into existence together with a real booking).
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var package = new PatientSessionPackage
        {
            Id = Guid.CreateVersion7(),
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            TotalSessions = request.TotalSessions,
            SessionDuration = request.SessionDuration,
            ScheduledSessions = 0,
            RemainingSessions = request.TotalSessions,
            Status = PackageStatus.Active,
            SessionsPerWeek = request.SessionsPerWeek,
            MinimumGapBetweenSessionsDays = request.MinimumGapBetweenSessionsDays,
            PreferredTimeOfDay = request.PreferredTimeOfDay,
            PreferredDays = request.PreferredDays,
            Priority = request.Priority
        };

        _context.Set<PatientSessionPackage>().Add(package);
        await _context.SaveChangesAsync(cancellationToken);

        // NOTE: requires PackageId to be added to CreateAppointmentRequest and mapped
        // through in AppointmentService.CreateAsync — see accompanying diff notes.
        var bookingResult = await _appointmentService.CreateAsync(new CreateAppointmentRequest
        {
            DoctorId = request.DoctorId,
            PatientId = request.PatientId,
            SlotStart = request.SlotStart,
            SlotEnd = request.SlotEnd,
            PackageId = package.Id
        }, cancellationToken);

        if (bookingResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<PatientSessionPackageDto>(bookingResult.Error);
        }

        package.ScheduledSessions = 1;
        package.RemainingSessions = request.TotalSessions - 1;
        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new PatientSessionPackageDto
        {
            Id = package.Id,
            PatientId = package.PatientId,
            DoctorId = package.DoctorId,
            TotalSessions = package.TotalSessions,
            ScheduledSessions = package.ScheduledSessions,
            RemainingSessions = package.RemainingSessions,
            Status = package.Status,
            FirstScheduleSlotId = bookingResult.Value.Id
        });
    }
    public async Task<ScheduleSlotResult?> GetFirstBookedSessionForPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _context.ScheduleSlots
            .Where(s => s.PatientId == patientId && s.Status == SlotStatus.Booked)
            .OrderBy(s => s.SlotStart)
            .Select(s => new ScheduleSlotResult(s.PatientId, s.SlotStart, s.SlotEnd))
            .FirstOrDefaultAsync(cancellationToken);
    }
}