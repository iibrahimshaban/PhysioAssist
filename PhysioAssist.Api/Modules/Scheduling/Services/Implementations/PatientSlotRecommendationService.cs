using PhysioAssist.Api.Modules.Scheduling.DTO.AgentDtos;
using PhysioAssist.Api.Modules.Scheduling.helpers;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;
using PhysioAssist.Api.Shared.Interfaces.Ingestion;
using PhysioAssist.Api.Shared.Interfaces.Scheduling;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Implementations;

public class PatientSlotRecommendationService(
        IQueryTranslationService translationService,
        IPatientTimePreferenceParser preferenceParser,
        IDoctorScheduleRecommendationService recommendationService) : IPatientSlotRecommendationService
{
    private readonly IQueryTranslationService _translationService = translationService;
    private readonly IPatientTimePreferenceParser _preferenceParser = preferenceParser;
    private readonly IDoctorScheduleRecommendationService _recommendationService = recommendationService;

    // Same Egypt-offset convention used throughout the module.
    private static readonly TimeSpan EgyptOffset = TimeSpan.FromHours(3);

    public async Task<Result<IReadOnlyList<SlotCandidateDto>>> GetTopRecommendedSlotsAsync(
        Guid doctorId,
        TimeSpan requestedDuration,
        string patientFreeTimeText,
        int topN = 5,
        CancellationToken cancellationToken = default)
    {
        // Handles Arabic (or any other language) transparently — already a no-op-safe
        // fallback to the original text on empty/failed translation, per its own design.
        var englishText = await _translationService.TranslateToEnglishAsync(patientFreeTimeText, cancellationToken);

        var preferenceResult = await _preferenceParser.ParseAsync(englishText, cancellationToken);

        if (preferenceResult.IsFailure)
            return Result.Failure<IReadOnlyList<SlotCandidateDto>>(preferenceResult.Error);

        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(EgyptOffset).Date);
        var (rangeStart, rangeEnd) = TimePreferenceResolver.ResolveDateRange(preferenceResult.Value, today);

        var from = new DateTimeOffset(rangeStart.ToDateTime(TimeOnly.MinValue), EgyptOffset);
        var to = new DateTimeOffset(rangeEnd.ToDateTime(TimeOnly.MaxValue), EgyptOffset);

        var slotsResult = await _recommendationService.GetRecommendedSlotsAsync(
            doctorId,
            requestedDuration,
            from,
            to,
            preferenceResult.Value.PreferredTimeFrom,
            preferenceResult.Value.PreferredTimeTo,
            cancellationToken);

        if (slotsResult.IsFailure)
            return Result.Failure<IReadOnlyList<SlotCandidateDto>>(slotsResult.Error);

        // Already ranked by Score desc, Start asc — just take the top N for the frontend cards.
        var topSlots = slotsResult.Value.Take(topN).ToList();

        return Result.Success<IReadOnlyList<SlotCandidateDto>>(topSlots);
    }
}
