using PhysioAssist.Api.Modules.Scheduling.DTO.AgentDtos;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;

public interface IPatientSlotRecommendationService
{
    Task<Result<IReadOnlyList<SlotCandidateDto>>> GetTopRecommendedSlotsAsync(
            Guid doctorId,
            TimeSpan requestedDuration,
            string patientFreeTimeText,
            int topN = 5,
            CancellationToken cancellationToken = default);
}
