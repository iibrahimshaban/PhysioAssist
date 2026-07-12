using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Interfaces;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Implementations;

public class ScheduleSlotQueryService(ApplicationDbContext context) : IScheduleSlotQueryService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<List<ScheduleSlotResult>> GetTodaySlotsForDoctorAsync(Guid doctorId, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        return await _context.ScheduleSlots
            .Where(s =>
                s.DoctorId == doctorId &&
                s.SlotStart >= today &&
                s.SlotStart < tomorrow &&
                (s.Status == SlotStatus.Booked || s.Status == SlotStatus.Completed))
            .OrderBy(s => s.SlotStart)
            .Select(s => new ScheduleSlotResult(s.PatientId, s.SlotStart, s.SlotEnd))
            .ToListAsync(ct);
    }
}