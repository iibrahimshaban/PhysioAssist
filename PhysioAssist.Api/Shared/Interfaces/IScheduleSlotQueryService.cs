namespace PhysioAssist.Api.Shared.Interfaces;

public record ScheduleSlotResult(Guid PatientId, DateTimeOffset SlotStart, DateTimeOffset SlotEnd);

public interface IScheduleSlotQueryService
{
    Task<List<ScheduleSlotResult>> GetUpcomingSlotsForDoctorAsync(Guid doctorId, CancellationToken ct = default);
}