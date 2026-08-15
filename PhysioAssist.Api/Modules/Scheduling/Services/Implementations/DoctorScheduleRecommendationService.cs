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

    // Local record — not persisted, just carries the doctor's real booked/completed
    // slot boundaries through this file for the overlap check below.
    private readonly record struct BookedRange(DateTimeOffset Start, DateTimeOffset End);

    public async Task<Result<IReadOnlyList<SlotCandidateDto>>> GetRecommendedSlotsAsync(
    Guid doctorId,
    TimeSpan requestedDuration,
    DateTimeOffset? from = null,
    DateTimeOffset? to = null,
    TimeOnly? preferredTimeFrom = null,
    TimeOnly? preferredTimeTo = null,
    bool allowSameDayBooking = false,
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

        var nowInEgypt = DateTimeOffset.UtcNow.ToOffset(EgyptOffset);
        var today = DateOnly.FromDateTime(nowInEgypt.Date);
        var nowTimeOfDay = TimeOnly.FromDateTime(nowInEgypt.DateTime);

        var rangeStart = from ?? DateTimeOffset.UtcNow;
        var rangeEnd = to ?? rangeStart.AddDays(DefaultMaxDaysOutForExactMatch);

        var realBookedSlots = await _context.Set<ScheduleSlot>()
            .Where(s => s.DoctorId == doctorId
                        && (s.Status == SlotStatus.Booked || s.Status == SlotStatus.Completed)
                        && s.SlotStart < rangeEnd
                        && s.SlotEnd > rangeStart)
            .Select(s => new BookedRange(s.SlotStart, s.SlotEnd))
            .ToListAsync(cancellationToken);

        var candidates = new List<SlotCandidateDto>();

        foreach (var day in availabilityResult.Value)
        {
            var isToday = day.Date == today;

            if (day.Date < today)
                continue;
            if (isToday && !allowSameDayBooking)
                continue;

            var daysOut = day.Date.DayNumber - today.DayNumber;
            var isBeyondHorizon = daysOut > maxDaysOutForExactMatch;

            foreach (var interval in day.Intervals)
            {
                var effectiveStart = preferredTimeFrom.HasValue && preferredTimeFrom.Value > interval.Start
                    ? preferredTimeFrom.Value
                    : interval.Start;

                // Same-day: don't offer a slot that's already in the past.
                if (isToday && nowTimeOfDay > effectiveStart)
                    effectiveStart = nowTimeOfDay;

                var effectiveEnd = preferredTimeTo.HasValue && preferredTimeTo.Value < interval.End
                    ? preferredTimeTo.Value
                    : interval.End;

                if (effectiveStart >= effectiveEnd)
                    continue;

                var availableDuration = effectiveEnd - effectiveStart;
                if (availableDuration <= TimeSpan.Zero)
                    continue;

                var slotStart = effectiveStart;

                while (slotStart < effectiveEnd)
                {
                    var remaining = effectiveEnd - slotStart;

                    if (remaining >= requestedDuration)
                    {
                        var slotEnd = slotStart.Add(requestedDuration);

                        if (!OverlapsRealBooking(day.Date, slotStart, slotEnd, realBookedSlots))
                        {
                            candidates.Add(BuildCandidate(
                                day.Date, slotStart, slotEnd,
                                requestedDuration, requestedDuration,
                                SlotFitType.Exact, TimeSpan.Zero, isBeyondHorizon));
                        }

                        slotStart = slotEnd;
                    }
                    else
                    {
                        var shortfall = requestedDuration - remaining;

                        if (allowShorterSlots && shortfall <= maxShortfallTolerance
                            && !OverlapsRealBooking(day.Date, slotStart, effectiveEnd, realBookedSlots))
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

    private static bool OverlapsRealBooking(
        DateOnly date, TimeOnly start, TimeOnly end, IReadOnlyList<BookedRange> bookedSlots)
    {
        var candidateStart = new DateTimeOffset(date.ToDateTime(start), EgyptOffset);
        var candidateEnd = new DateTimeOffset(date.ToDateTime(end), EgyptOffset);

        // Standard half-open interval overlap — same shape as HasOverlapAsync — but
        // computed entirely from real, correctly-offset DateTimeOffset values on
        // both sides, so it's immune to the upstream TimeOnly-conversion bug.
        foreach (var booked in bookedSlots)
        {
            if (candidateStart < booked.End && candidateEnd > booked.Start)
                return true;
        }

        return false;
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

    private static double ComputeScore(SlotFitType fitType, TimeSpan gap, bool isBeyondHorizon)
    {
        var score = fitType switch
        {
            SlotFitType.Exact => 1.0,
            SlotFitType.LongerThanRequested => 0.95,
            _ => Math.Clamp(0.9 - (gap.TotalMinutes / 60.0) * 0.3, 0.1, 0.9)
        };

        if (isBeyondHorizon)
            score -= 0.15;

        return Math.Clamp(score, 0, 1);
    }
}