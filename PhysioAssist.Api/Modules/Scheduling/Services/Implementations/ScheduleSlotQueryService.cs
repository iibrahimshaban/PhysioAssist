using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.Errors;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;
using PhysioAssist.Api.Shared.Dtos.Patient;
using PhysioAssist.Api.Shared.Dtos.Schedule;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Implementations;

public class ScheduleSlotQueryService(ApplicationDbContext _context, IPatientSessionPackageAdjustmentService _patientSessionPackageAdjustmentService) : IScheduleSlotQueryService
{

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

    public async Task<Result<PatientScheduleOverviewDto>> GetScheduleOverviewAsync(
       Guid patientId, CancellationToken cancellationToken = default)
    {
        var latestPackage = await _context.Set<PatientSessionPackage>()
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestPackage is null)
            return Result.Success(PatientScheduleOverviewDto.Empty());

        var slots = await _context.Set<ScheduleSlot>()
            .Where(s => s.PackageId == latestPackage.Id && s.Status != SlotStatus.Cancelled)
            .OrderBy(s => s.SlotStart)
            .ToListAsync(cancellationToken);

        var sessionItems = slots
            .Select((s, index) => new PatientSessionListItemDto
            {
                SlotId = s.Id,
                SessionNumber = index + 1,
                SlotStart = s.SlotStart,
                SlotEnd = s.SlotStart.Add(latestPackage.SessionDuration),
                Status = s.Status
            })
            .ToList();

        return Result.Success(new PatientScheduleOverviewDto
        {
            HasPackage = true,
            PackageId = latestPackage.Id,
            PackageStatus = latestPackage.Status,
            TotalSessions = latestPackage.TotalSessions,
            CompletedSessions = sessionItems.Count(s => s.Status == SlotStatus.Completed),
            RemainingSessions = latestPackage.RemainingSessions,
            UpcomingScheduledCount = sessionItems.Count(s => s.Status == SlotStatus.Booked),
            Sessions = sessionItems
        });
    }
    public async Task<Result> MarkCompletedAsync(Guid scheduleSlotId, CancellationToken cancellationToken = default)
    {
        var slot = await _context.Set<ScheduleSlot>()
            .FirstOrDefaultAsync(s => s.Id == scheduleSlotId, cancellationToken);

        if (slot is null)
            return Result.Failure(SchedulingErrors.SlotNotFound);

        if (slot.Status == SlotStatus.Completed)
            return Result.Success();

        if (slot.Status != SlotStatus.Booked)
            return Result.Failure(SchedulingErrors.SlotNotInBookedState);

        slot.Status = SlotStatus.Completed;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> MarkNoShowAsync(Guid scheduleSlotId, bool countsAsUsed, CancellationToken cancellationToken = default)
    {
        var slot = await _context.Set<ScheduleSlot>()
            .FirstOrDefaultAsync(s => s.Id == scheduleSlotId, cancellationToken);

        if (slot is null)
            return Result.Failure(SchedulingErrors.SlotNotFound);

        if (slot.Status != SlotStatus.Booked)
            return Result.Failure(SchedulingErrors.SlotNotInBookedState);

        slot.Status = SlotStatus.NoShow;
        await _context.SaveChangesAsync(cancellationToken);

        if (!countsAsUsed && slot.PackageId.HasValue)
        {
            var extendResult = await _patientSessionPackageAdjustmentService.ExtendByOneSessionAsync(slot.PackageId.Value, cancellationToken);
            if (extendResult.IsFailure)
                return extendResult;
        }

        return Result.Success();
    }
    public async Task<Result<IReadOnlyList<Guid>>> GetPriorSlotIdsInPackageAsync(
        Guid scheduleSlotId, CancellationToken cancellationToken = default)
    {
        var current = await _context.Set<ScheduleSlot>()
            .Where(s => s.Id == scheduleSlotId)
            .Select(s => new { s.PackageId, s.SlotStart })
            .FirstOrDefaultAsync(cancellationToken);

        if (current is null || current.PackageId is null)
            return Result.Success<IReadOnlyList<Guid>>([]);

        var priorSlotIds = await _context.Set<ScheduleSlot>()
            .Where(s => s.PackageId == current.PackageId && s.SlotStart < current.SlotStart)
            .OrderByDescending(s => s.SlotStart)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<Guid>>(priorSlotIds);
    }
    public async Task<Dictionary<Guid, ScheduleSlotSummary>> GetSlotSummariesByIdsAsync(IEnumerable<Guid> slotIds, CancellationToken cancellationToken = default)
    {
        var ids = slotIds.Distinct().ToList();

        return await _context.Set<ScheduleSlot>()
            .Where(s => ids.Contains(s.Id))
            .Select(s => new ScheduleSlotSummary(s.Id, s.SlotStart, s.SlotEnd, s.Status))
            .ToDictionaryAsync(s => s.SlotId, cancellationToken);
    }
}