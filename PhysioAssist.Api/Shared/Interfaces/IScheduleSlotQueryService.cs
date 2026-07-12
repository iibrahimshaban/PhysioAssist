namespace PhysioAssist.Api.Shared.Interfaces;

public record ScheduleSlotResult(Guid PatientId, DateTime SlotStart, DateTime SlotEnd);

public interface IScheduleSlotQueryService
{
    Task<List<ScheduleSlotResult>> GetTodaySlotsForDoctorAsync(Guid doctorId, CancellationToken ct = default);
}