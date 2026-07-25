using PhysioAssist.Api.Shared.Dtos.Schedule;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;

public interface IDoctorScheduleRecommendationService
{
    Task<Result<IReadOnlyList<SlotCandidateDto>>> GetRecommendedSlotsAsync(
            Guid doctorId,
            TimeSpan requestedDuration,
            DateTimeOffset? from = null,
            DateTimeOffset? to = null,
            TimeOnly? preferredTimeFrom = null,
            TimeOnly? preferredTimeTo = null,
            CancellationToken cancellationToken = default);
}
