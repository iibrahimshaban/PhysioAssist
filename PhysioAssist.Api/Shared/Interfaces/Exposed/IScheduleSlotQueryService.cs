using PhysioAssist.Api.Shared.Dtos.Schedule;

namespace PhysioAssist.Api.Shared.Interfaces.Exposed;

public record ScheduleSlotResult(Guid PatientId, DateTimeOffset SlotStart, DateTimeOffset SlotEnd);

public interface IScheduleSlotQueryService
{
    Task<List<ScheduleSlotResult>> GetUpcomingSlotsForDoctorAsync(Guid doctorId, CancellationToken ct = default);
    Task<Result<PatientSessionPackageDto>> CreatePackageWithFirstBookingAsync(CreatePackageWithFirstBookingRequest request,
            CancellationToken cancellationToken = default);

    Task<ScheduleSlotResult?> GetFirstBookedSessionForPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
}