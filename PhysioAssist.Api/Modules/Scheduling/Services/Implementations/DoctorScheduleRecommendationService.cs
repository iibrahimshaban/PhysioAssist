using PhysioAssist.Api.Modules.Scheduling.DTO.AgentDtos;
using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;
using PhysioAssist.Api.Shared.Dtos.Schedule;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Implementations;

public class DoctorScheduleRecommendationService(
        IAppointmentService appointmentService,
        ApplicationDbContext context) : IDoctorScheduleRecommendationService
{
    private readonly IAppointmentService _appointmentService = appointmentService;
    private readonly ApplicationDbContext _context = context;

    private static readonly TimeSpan EgyptOffset = TimeSpan.FromHours(3);

    private static readonly TimeSpan DefaultMaxShortfallTolerance = TimeSpan.FromMinutes(15);
    private const int DefaultMaxDaysOutForExactMatch = 7;
    private const bool DefaultAllowShorterSlots = true;

    public async Task<Result<IReadOnlyList<SlotCandidateDto>>> GetRecommendedSlotsAsync(
        Guid doctorId,
        TimeSpan requestedDuration,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        TimeOnly? preferredTimeFrom = null,
        TimeOnly? preferredTimeTo = null,
        CancellationToken cancellationToken = default)
    {
        var availabilityResult = await _appointmentService.GetAvailabilityRangeAsync(doctorId, from, to, cancellationToken);
        if (availabilityResult.IsFailure)
            return Result.Failure<IReadOnlyList<SlotCandidateDto>>(availabilityResult.Error);

        var preference = await _context.Set<DoctorSchedulingPreference>()
            .FirstOrDefaultAsync(p => p.DoctorId == doctorId, cancellationToken);

        var maxShortfallTolerance = preference?.MaxShortfallTolerance ?? DefaultMaxShortfallTolerance;
        var maxDaysOutForExactMatch = preference?.MaxDaysOutForExactMatch ?? DefaultMaxDaysOutForExactMatch;
        var allowShorterSlots = preference?.AllowShorterSlots ?? DefaultAllowShorterSlots;

        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(EgyptOffset).Date);

        var candidates = new List<SlotCandidateDto>();

        foreach (var day in availabilityResult.Value)
        {
            // Never offer same-day appointments — the earliest a slot can be recommended
            // is the doctor's next working day, even if the doctor still has free time
            // left later today. This is an absolute clinic policy, not a preference, so
            // it's enforced here regardless of what date range the caller passed in.
            if (day.Date <= today)
                continue;

            var daysOut = day.Date.DayNumber - today.DayNumber;
            var isBeyondHorizon = daysOut > maxDaysOutForExactMatch;

            foreach (var interval in day.Intervals)
            {
                // Narrow the free interval down to whatever overlaps the patient's
                // preferred time-of-day window, if one was given. If the window doesn't
                // touch this interval at all, there's nothing to offer here.
                var effectiveStart = preferredTimeFrom.HasValue && preferredTimeFrom.Value > interval.Start
                    ? preferredTimeFrom.Value
                    : interval.Start;

                var effectiveEnd = preferredTimeTo.HasValue && preferredTimeTo.Value < interval.End
                    ? preferredTimeTo.Value
                    : interval.End;

                if (effectiveStart >= effectiveEnd)
                    continue;

                var availableDuration = effectiveEnd - effectiveStart;
                if (availableDuration <= TimeSpan.Zero)
                    continue;

                // Walk the (possibly narrowed) interval in requestedDuration-sized steps so a
                // single long free block yields multiple same-day candidates instead of just one
                // slot pinned to the start of the interval. Each full-duration step is an exact
                // fit; whatever is left over at the end (shorter than requestedDuration) is
                // offered only as a near-miss, same as before.
                var slotStart = effectiveStart;

                while (slotStart < effectiveEnd)
                {
                    var remaining = effectiveEnd - slotStart;

                    if (remaining >= requestedDuration)
                    {
                        var slotEnd = slotStart.Add(requestedDuration);

                        candidates.Add(BuildCandidate(
                            day.Date, slotStart, slotEnd,
                            requestedDuration, requestedDuration,
                            SlotFitType.Exact, TimeSpan.Zero, isBeyondHorizon));

                        slotStart = slotEnd;
                    }
                    else
                    {
                        // Trailing remainder shorter than the requested duration — only offer
                        // it as a near-miss if the doctor's preference allows shorter slots and
                        // the shortfall is within their tolerance.
                        var shortfall = requestedDuration - remaining;

                        if (allowShorterSlots && shortfall <= maxShortfallTolerance)
                        {
                            candidates.Add(BuildCandidate(
                                day.Date, slotStart, effectiveEnd,
                                remaining, requestedDuration,
                                SlotFitType.ShorterThanRequested, shortfall, isBeyondHorizon));
                        }

                        break;
                    }
                }
            }
        }

        var ranked = candidates
            .OrderByDescending(c => c.Score)
            .ThenBy(c => c.Start)
            .ToList();

        return Result.Success<IReadOnlyList<SlotCandidateDto>>(ranked);
    }

    private static SlotCandidateDto BuildCandidate(
        DateOnly date, TimeOnly start, TimeOnly end,
        TimeSpan availableDuration, TimeSpan requestedDuration,
        SlotFitType fitType, TimeSpan gap, bool isBeyondHorizon)
    {
        var slotStart = new DateTimeOffset(date.ToDateTime(start), EgyptOffset);
        var slotEnd = new DateTimeOffset(date.ToDateTime(end), EgyptOffset);

        return new SlotCandidateDto
        {
            Start = slotStart,
            End = slotEnd,
            AvailableDuration = availableDuration,
            RequestedDuration = requestedDuration,
            FitType = fitType,
            Gap = gap,
            IsBeyondPreferredHorizon = isBeyondHorizon,
            Score = ComputeScore(fitType, gap, isBeyondHorizon)
        };
    }

    // Simple, tunable v1 scoring: exact fits score highest; near-misses lose a bit
    // per minute of shortfall; anything beyond the doctor's preferred horizon takes
    // a flat penalty either way. Revisit once real usage data suggests better weights,
    // and once ISlotSignalPlugin implementations (e.g. PatientHistoryPlugin) start
    // adjusting this score further upstream in the agent.
    private static double ComputeScore(SlotFitType fitType, TimeSpan gap, bool isBeyondHorizon)
    {
        var score = fitType switch
        {
            SlotFitType.Exact => 1.0,
            SlotFitType.LongerThanRequested => 0.95,
            // Near-miss — the only branch penalized per minute of shortfall.
            _ => Math.Clamp(0.9 - (gap.TotalMinutes / 60.0) * 0.3, 0.1, 0.9)
        };

        if (isBeyondHorizon)
            score -= 0.15;

        return Math.Clamp(score, 0, 1);
    }
}