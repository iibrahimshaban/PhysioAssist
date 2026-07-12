using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Interfaces;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Implementations;

public class ScheduleSlotQueryService(ApplicationDbContext context) : IScheduleSlotQueryService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<List<ScheduleSlotResult>> GetUpcomingSlotsForDoctorAsync(Guid doctorId, CancellationToken ct = default)
    {
        return await _context.ScheduleSlots
            .Where(s =>
                s.DoctorId == doctorId &&
                s.SlotStart >= DateTime.UtcNow &&
                s.Status == SlotStatus.Booked)
            .OrderBy(s => s.SlotStart)
            .Select(s => new ScheduleSlotResult(s.PatientId, s.SlotStart, s.SlotEnd))
            .ToListAsync(ct);
    }
}