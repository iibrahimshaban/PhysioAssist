using PhysioAssist.Api.Shared.Dtos.Package;
using PhysioAssist.Api.Shared.Dtos.Schedule;

namespace PhysioAssist.Api.Shared.Interfaces.Exposed;

public interface IPatientSessionSchedulingService
{
    Task<Result<SessionBookingRoundDto>> GetNextSessionCandidatesAsync(
         Guid packageId,
         string? patientFreeTimeOverride = null,
         bool persistFreeTimeOverride = false,
         TimeSpan? sessionDurationOverride = null,
         int? sessionsPerWeekOverride = null,
         int? minimumGapOverrideDays = null,
         PreferredTimeOfDay? preferredTimeOfDayOverride = null,
         DaysOfWeekFlags? preferredDaysOverride = null,
         CancellationToken cancellationToken = default);

    Task<Result<ScheduleSlotDto>> ConfirmSessionSlotAsync(
        Guid packageId,
        SlotCandidateDto chosenSlot,
        CancellationToken cancellationToken = default);

    Task<Result<PatientSessionPackageDto>> CreatePackageWithFirstBookingAsync(
        CreatePackageWithFirstBookingRequest request,
        CancellationToken cancellationToken = default);

    Task<ScheduleSlotResult?> GetFirstBookedSessionForPatientAsync(
        Guid patientId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<SlotCandidateDto>>> GetTopRecommendedSlotsAsync(
        Guid doctorId,
        TimeSpan requestedDuration,
        Guid patientId,
        int topN = 5,
        string? patientFreeTimeOverride = null,
        bool allowSameDayBooking = false,
        CancellationToken cancellationToken = default);
}
