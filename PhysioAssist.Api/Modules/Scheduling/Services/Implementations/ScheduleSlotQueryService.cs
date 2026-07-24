using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Shared.Dtos.Patient;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Implementations;

public class ScheduleSlotQueryService(ApplicationDbContext context) : IScheduleSlotQueryService
{
    private readonly ApplicationDbContext _context = context;

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
        var latestPackage = await context.Set<PatientSessionPackage>()
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestPackage is null)
            return Result.Success(PatientScheduleOverviewDto.Empty());

        var slots = await context.Set<ScheduleSlot>()
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

        var now = DateTimeOffset.UtcNow;

        return Result.Success(new PatientScheduleOverviewDto
        {
            HasPackage = true,
            PackageId = latestPackage.Id,
            PackageStatus = latestPackage.Status,
            TotalSessions = latestPackage.TotalSessions,
            CompletedSessions = sessionItems.Count(s => s.SlotStart < now),
            RemainingSessions = latestPackage.RemainingSessions,
            UpcomingScheduledCount = sessionItems.Count(s => s.SlotStart >= now),
            Sessions = sessionItems
        });
    }

}