using PhysioAssist.Api.Shared.Dtos.Schedule;

namespace PhysioAssist.Api.Shared.Interfaces.Exposed;

public interface IPatientSessionSchedulingService
{
    /// <summary>
    /// Creates a PatientSessionPackage. If request.FirstSessionSlot is set (doctor's
    /// own "Confirm booking" path), the first slot is booked in the same call via
    /// ConfirmSessionSlotAsync, leaving ScheduledSessions = 1. Otherwise the package
    /// is created with ScheduledSessions = 0 (receptionist path — nothing booked yet).
    /// </summary>
    Task<Result<CreateSessionPackageResult>> CreatePackageAsync(
        CreateSessionPackageRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns up to 5 ranked candidate slots for the NEXT unscheduled session in the
    /// package, respecting the package's weekly quota, minimum gap since the last
    /// actually-confirmed session, and the doctor's real working-day cycle.
    /// </summary>
    Task<Result<SessionBookingRoundDto>> GetNextSessionCandidatesAsync(
        Guid packageId,
        string? patientFreeTimeOverride = null,
        bool persistFreeTimeOverride = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Books the chosen slot via IAppointmentService.CreateAsync, links it to the
    /// package, and updates ScheduledSessions/RemainingSessions (and Status, once
    /// RemainingSessions hits zero).
    /// </summary>
    Task<Result<ScheduleSlotDto>> ConfirmSessionSlotAsync(
        Guid packageId,
        SlotCandidateDto chosenSlot,
        CancellationToken cancellationToken = default);

    Task<Result<PatientSessionPackageDto>> CreatePackageWithFirstBookingAsync(CreatePackageWithFirstBookingRequest request,
            CancellationToken cancellationToken = default);

    Task<ScheduleSlotResult?> GetFirstBookedSessionForPatientAsync(Guid patientId, CancellationToken cancellationToken = default);

    public Task<Result<IReadOnlyList<SlotCandidateDto>>> GetTopRecommendedSlotsAsync(
        Guid doctorId,
        TimeSpan requestedDuration,
        Guid patientId,
        int topN = 5,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetPackageDoctorIdAsync(Guid packageId, CancellationToken cancellationToken = default);
    Task<Result<PatientSessionPackageSummaryDto>> GetPackageSummaryAsync(Guid packageId, CancellationToken cancellationToken = default);
}
