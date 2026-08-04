using PhysioAssist.Api.Shared.Dtos.Patient;
using PhysioAssist.Api.Shared.Dtos.Schedule;
namespace PhysioAssist.Api.Shared.Interfaces.Exposed;

public record ScheduleSlotResult(Guid? PatientId, DateTimeOffset SlotStart, DateTimeOffset SlotEnd);

public interface IScheduleSlotQueryService
{
    Task<List<ScheduleSlotResult>> GetUpcomingSlotsForDoctorAsync(Guid doctorId, CancellationToken ct = default);
    Task<Result<PatientScheduleOverviewDto>> GetScheduleOverviewAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<Result> MarkNoShowAsync(Guid scheduleSlotId, bool countsAsUsed, CancellationToken cancellationToken = default);
    Task<Result> MarkCompletedAsync(Guid scheduleSlotId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<Guid>>> GetPriorSlotIdsInPackageAsync(Guid scheduleSlotId, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, ScheduleSlotSummary>> GetSlotSummariesByIdsAsync(IEnumerable<Guid> slotIds, CancellationToken cancellationToken = default);

}