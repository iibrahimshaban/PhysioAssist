using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.SessionModule.Entities;
using PhysioAssist.Api.Shared.Dtos.Session;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Implementations;

public class PackageSchedulingStatusService(ApplicationDbContext context, ISessionQueryService _sessionQueryService) : IPackageSchedulingStatusService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<Result<NextSessionBookingContextDto>> GetContextAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var scheduleSlotId = await _context.Set<Session>()
            .Where(s => s.Id == sessionId)
            .Select(s => s.ScheduleSlotId)
            .FirstOrDefaultAsync(cancellationToken);

        if (scheduleSlotId is null)
            return Result.Success(new NextSessionBookingContextDto { State = NextSessionBookingState.NotApplicable });

        var slot = await _context.Set<ScheduleSlot>()
            .Where(s => s.Id == scheduleSlotId.Value)
            .Select(s => new { s.PackageId, s.SlotStart })
            .FirstOrDefaultAsync(cancellationToken);

        if (slot?.PackageId is null)
            return Result.Success(new NextSessionBookingContextDto { State = NextSessionBookingState.NotApplicable });

        var nextSlotStart = await _context.Set<ScheduleSlot>()
            .Where(s => s.PackageId == slot.PackageId
                        && s.Status != SlotStatus.Cancelled
                        && s.SlotStart > slot.SlotStart)
            .OrderBy(s => s.SlotStart)
            .Select(s => (DateTimeOffset?)s.SlotStart)
            .FirstOrDefaultAsync(cancellationToken);

        if (nextSlotStart.HasValue)
            return Result.Success(new NextSessionBookingContextDto
            {
                State = NextSessionBookingState.NotApplicable,
                PackageId = slot.PackageId,
                NextScheduledSlotStart = nextSlotStart
            });

        var remainingSessions = await _context.Set<PatientSessionPackage>()
            .Where(p => p.Id == slot.PackageId)
            .Select(p => p.RemainingSessions)
            .FirstOrDefaultAsync(cancellationToken);

        var state = remainingSessions > 0
            ? NextSessionBookingState.CanBookNext
            : NextSessionBookingState.LastSessionDecisionNeeded;

        return Result.Success(new NextSessionBookingContextDto
        {
            State = state,
            PackageId = slot.PackageId
        });
    }
}
