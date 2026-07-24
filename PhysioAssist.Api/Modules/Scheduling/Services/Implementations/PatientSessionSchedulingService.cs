using PhysioAssist.Api.Modules.PatientModule.Entities;
using PhysioAssist.Api.Modules.Scheduling.DTO;
using PhysioAssist.Api.Modules.Scheduling.DTO.AgentDtos;
using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.Errors;
using PhysioAssist.Api.Modules.Scheduling.helpers;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;
using PhysioAssist.Api.Shared.Dtos.Schedule;


namespace PhysioAssist.Api.Modules.Scheduling.Services.Implementations;

public class PatientSessionSchedulingService(
        ApplicationDbContext context,
        IDoctorScheduleRecommendationService recommendationService,
        IAppointmentService appointmentService,
        IPatientQueryService _patientQueryService) : IPatientSessionSchedulingService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IDoctorScheduleRecommendationService _recommendationService = recommendationService;
    private readonly IAppointmentService _appointmentService = appointmentService;

    private static readonly TimeSpan EgyptOffset = TimeSpan.FromHours(3);
    private const int CandidatesPerSession = 5;

    public async Task<Result<CreateSessionPackageResult>> CreatePackageAsync(
        CreateSessionPackageRequest request,
        CancellationToken cancellationToken = default)
    {
        var package = new PatientSessionPackage
        {
            Id = Guid.CreateVersion7(),
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            TotalSessions = request.TotalSessions,
            SessionDuration = request.SessionDuration,
            ScheduledSessions = 0,
            RemainingSessions = request.TotalSessions,
            Status = PackageStatus.Active,
            SessionsPerWeek = request.SessionsPerWeek,
            MinimumGapBetweenSessionsDays = request.MinimumGapBetweenSessionsDays,
            PreferredTimeOfDay = request.PreferredTimeOfDay,
            PreferredDays = request.PreferredDays,
            Priority = request.Priority
        };

        _context.Set<PatientSessionPackage>().Add(package);
        await _context.SaveChangesAsync(cancellationToken);

        if (request.FirstSessionSlot is null)
            return Result.Success(new CreateSessionPackageResult
            {
                PackageId = package.Id,
                ScheduledSessions = 0,
                FirstSessionSlot = null
            });

        // Doctor already picked a slot on the same screen — book it right away
        // through the exact same path the receptionist will use for sessions 2..N,
        // so ScheduledSessions/RemainingSessions bookkeeping only lives in one place.
        var confirmResult = await ConfirmSessionSlotAsync(package.Id, request.FirstSessionSlot, cancellationToken);
        if (confirmResult.IsFailure)
            return Result.Failure<CreateSessionPackageResult>(confirmResult.Error);

        return Result.Success(new CreateSessionPackageResult
        {
            PackageId = package.Id,
            ScheduledSessions = 1,
            FirstSessionSlot = confirmResult.Value
        });
    }

    public async Task<Result<SessionBookingRoundDto>> GetNextSessionCandidatesAsync(Guid packageId,
    string? patientFreeTimeOverride = null, bool persistFreeTimeOverride = false,
    CancellationToken cancellationToken = default)
    {
        var package = await _context.Set<PatientSessionPackage>()
            .FirstOrDefaultAsync(p => p.Id == packageId, cancellationToken);

        if (package is null)
            return Result.Failure<SessionBookingRoundDto>(SchedulingErrors.PackageNotFound);

        if (package.RemainingSessions <= 0)
            return Result.Failure<SessionBookingRoundDto>(SchedulingErrors.PackageAlreadyComplete);

        var patientFreeTimeText = string.IsNullOrWhiteSpace(patientFreeTimeOverride)
            ? await _context.Set<Patient>()
                .Where(p => p.Id == package.PatientId)
                .Select(p => p.PatientFreeTime)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty
            : patientFreeTimeOverride;

        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(EgyptOffset).Date);

        var workingDaysList = await _context.Set<WorkingScheduleDay>()
            .Where(d => d.WorkingSchedule.DoctorId == package.DoctorId && d.WorkingSchedule.IsActive)
            .Select(d => d.Day)
            .ToListAsync(cancellationToken);

        var workingDays = workingDaysList.ToHashSet();

        // No active schedule at all — searching forward would just spin uselessly.
        if (workingDays.Count == 0)
            return Result.Failure<SessionBookingRoundDto>(SchedulingErrors.DoctorHasNoActiveSchedule);

        var packageAnchor = DateOnly.FromDateTime(package.CreatedAt.Add(EgyptOffset));
        var cycleIndex = (today.DayNumber - packageAnchor.DayNumber) / 7;
        var weeklyTargetCount = Math.Min(package.SessionsPerWeek, package.RemainingSessions);
        var sessionNumber = package.ScheduledSessions + 1;

        var existingSlotDates = (await _context.Set<ScheduleSlot>()
                .Where(s => s.PackageId == packageId && s.Status != SlotStatus.Cancelled)
                .Select(s => s.SlotStart)
                .ToListAsync(cancellationToken))
                .Select(d => DateOnly.FromDateTime(d.ToOffset(EgyptOffset).Date))
                .ToList();

        var lastConfirmed = existingSlotDates.Count > 0 ? existingSlotDates.Max() : (DateOnly?)null;

        const int MaxWeeksToSearch = 26; // ~6 months safety cap against unbounded search

        DateOnly weekStart = default, weekEnd = default, minDate = default;
        int scheduledThisWeek = 0;
        var found = false;

        for (var i = 0; i < MaxWeeksToSearch; i++)
        {
            var candidateStart = packageAnchor.AddDays((cycleIndex + i) * 7);
            var candidateEnd = WorkingWeekBoundaryHelper.GetCycleEnd(candidateStart, workingDays);

            if (candidateEnd < candidateStart)
                continue; // no working day inside this cycle — try next week

            var candidateScheduled = existingSlotDates.Count(d => d >= candidateStart && d <= candidateEnd);

            if (candidateScheduled >= weeklyTargetCount)
                continue; // this week's quota already filled — try next week

            var candidateMinDate = lastConfirmed.HasValue
                ? lastConfirmed.Value.AddDays(package.MinimumGapBetweenSessionsDays)
                : today;

            if (candidateMinDate < candidateStart) candidateMinDate = candidateStart;
            if (candidateMinDate < today) candidateMinDate = today; // never offer a past date

            if (candidateMinDate > candidateEnd)
                continue; // gap pushes past this week's end — try next week

            weekStart = candidateStart;
            weekEnd = candidateEnd;
            minDate = candidateMinDate;
            scheduledThisWeek = candidateScheduled;
            found = true;
            break;
        }

        if (!found)
        {
            var fallbackStart = packageAnchor.AddDays(cycleIndex * 7);
            return Result.Success(BuildRound(package, sessionNumber, weeklyTargetCount, 0,
                fallbackStart, fallbackStart, quotaMet: false, noRoom: true, candidates: [], patientFreeTimeText));
        }

        var (packageFrom, packageTo) = MapPreferredTimeOfDay(package.PreferredTimeOfDay);

        TimeOnly? patientFrom = null;
        TimeOnly? patientTo = null;

        var patientPreferenceResult = await _patientQueryService.ResolvePatientTimePreferenceAsync(
            package.PatientId, patientFreeTimeOverride, persistFreeTimeOverride, cancellationToken);

        if (patientPreferenceResult.IsSuccess)
        {
            patientFrom = patientPreferenceResult.Value.PreferredTimeFrom;
            patientTo = patientPreferenceResult.Value.PreferredTimeTo;
        }

        var (preferredFrom, preferredTo) = IntersectTimeWindows(packageFrom, packageTo, patientFrom, patientTo);

        var from = new DateTimeOffset(minDate.ToDateTime(TimeOnly.MinValue), EgyptOffset);
        var to = new DateTimeOffset(weekEnd.ToDateTime(TimeOnly.MaxValue), EgyptOffset);

        var slotsResult = await _recommendationService.GetRecommendedSlotsAsync(
            package.DoctorId, package.SessionDuration, from, to, preferredFrom, preferredTo, cancellationToken);

        if (slotsResult.IsFailure)
            return Result.Failure<SessionBookingRoundDto>(slotsResult.Error);

        var candidates = slotsResult.Value.AsEnumerable();

        if (package.PreferredDays != DaysOfWeekFlags.None)
            candidates = candidates.Where(c => MatchesPreferredDays(c.Start.DayOfWeek, package.PreferredDays));

        var topCandidates = candidates.Take(CandidatesPerSession).ToList();

        return Result.Success(BuildRound(package, sessionNumber, weeklyTargetCount, scheduledThisWeek,
            weekStart, weekEnd, quotaMet: false, noRoom: topCandidates.Count == 0, candidates: topCandidates,
            patientFreeTimeText));
    }

    public async Task<Result<ScheduleSlotDto>> ConfirmSessionSlotAsync(
        Guid packageId,
        SlotCandidateDto chosenSlot,
        CancellationToken cancellationToken = default)
    {
        var package = await _context.Set<PatientSessionPackage>()
            .FirstOrDefaultAsync(p => p.Id == packageId, cancellationToken);

        if (package is null)
            return Result.Failure<ScheduleSlotDto>(SchedulingErrors.PackageNotFound);

        if (package.RemainingSessions <= 0)
            return Result.Failure<ScheduleSlotDto>(SchedulingErrors.PackageAlreadyComplete);

        // Goes through your existing AppointmentService.CreateAsync, so the same
        // ValidateCreateAsync overlap/availability check and notification pipeline
        // apply here — this covers the "slot got taken between fetch and confirm"
        // race condition without any extra code in this service.
        var createResult = await _appointmentService.CreateAsync(new CreateAppointmentRequest
        {
            DoctorId = package.DoctorId,
            PatientId = package.PatientId,
            SlotStart = chosenSlot.Start,
            SlotEnd = chosenSlot.End,
            PackageId = package.Id
        }, cancellationToken);

        if (createResult.IsFailure)
            return Result.Failure<ScheduleSlotDto>(createResult.Error);

        package.ScheduledSessions++;
        package.RemainingSessions--;

        if (package.RemainingSessions == 0)
            package.Status = PackageStatus.Completed;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(createResult.Value);
    }

    public async Task<Result<PatientSessionPackageDto>> CreatePackageWithFirstBookingAsync(CreatePackageWithFirstBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.TotalSessions <= 0)
            return Result.Failure<PatientSessionPackageDto>(PatientSessionPackageErrors.InvalidTotalSessions);

        // Package and first booking must succeed or fail together — a package with
        // zero real bookings behind it shouldn't be able to exist, per how this was
        // designed (package only comes into existence together with a real booking).
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var package = new PatientSessionPackage
        {
            Id = Guid.CreateVersion7(),
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            TotalSessions = request.TotalSessions,
            SessionDuration = request.SessionDuration,
            ScheduledSessions = 0,
            RemainingSessions = request.TotalSessions,
            Status = PackageStatus.Active,
            SessionsPerWeek = request.SessionsPerWeek,
            MinimumGapBetweenSessionsDays = request.MinimumGapBetweenSessionsDays,
            PreferredTimeOfDay = request.PreferredTimeOfDay,
            PreferredDays = request.PreferredDays,
            Priority = request.Priority
        };

        _context.Set<PatientSessionPackage>().Add(package);
        await _context.SaveChangesAsync(cancellationToken);

        // NOTE: requires PackageId to be added to CreateAppointmentRequest and mapped
        // through in AppointmentService.CreateAsync — see accompanying diff notes.
        var bookingResult = await _appointmentService.CreateAsync(new CreateAppointmentRequest
        {
            DoctorId = request.DoctorId,
            PatientId = request.PatientId,
            SlotStart = request.SlotStart,
            SlotEnd = request.SlotEnd,
            PackageId = package.Id
        }, cancellationToken);

        if (bookingResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<PatientSessionPackageDto>(bookingResult.Error);
        }

        package.ScheduledSessions = 1;
        package.RemainingSessions = request.TotalSessions - 1;
        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new PatientSessionPackageDto
        {
            Id = package.Id,
            PatientId = package.PatientId,
            DoctorId = package.DoctorId,
            TotalSessions = package.TotalSessions,
            ScheduledSessions = package.ScheduledSessions,
            RemainingSessions = package.RemainingSessions,
            Status = package.Status,
            FirstScheduleSlotId = bookingResult.Value.Id
        });
    }
    public async Task<ScheduleSlotResult?> GetFirstBookedSessionForPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _context.ScheduleSlots
            .Where(s => s.PatientId == patientId && s.Status == SlotStatus.Booked)
            .OrderBy(s => s.SlotStart)
            .Select(s => new ScheduleSlotResult(s.PatientId, s.SlotStart, s.SlotEnd))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Result<IReadOnlyList<SlotCandidateDto>>> GetTopRecommendedSlotsAsync(
    Guid doctorId,
    TimeSpan requestedDuration,
    Guid patientId,
    int topN = 5,
    CancellationToken cancellationToken = default)
    {
        var preferenceResult = await _patientQueryService.GetPatientTimePreferenceAsync(patientId, cancellationToken);

        if (preferenceResult.IsFailure)
            return Result.Failure<IReadOnlyList<SlotCandidateDto>>(preferenceResult.Error);

        var preference = preferenceResult.Value;

        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(EgyptOffset).Date);

        var (rangeStart, rangeEnd) = TimePreferenceResolver.ResolveDateRange(
            new PatientTimePreferenceDto
            {
                DayToken = preference.DayToken,
                PreferredWeekdays = preference.PreferredWeekdays, // NEW — was missing
                PreferredTimeFrom = preference.PreferredTimeFrom,
                PreferredTimeTo = preference.PreferredTimeTo
            },
            today);

        var from = new DateTimeOffset(rangeStart.ToDateTime(TimeOnly.MinValue), EgyptOffset);
        var to = new DateTimeOffset(rangeEnd.ToDateTime(TimeOnly.MaxValue), EgyptOffset);

        var slotsResult = await _recommendationService.GetRecommendedSlotsAsync(
            doctorId,
            requestedDuration,
            from,
            to,
            preference.PreferredTimeFrom,
            preference.PreferredTimeTo,
            cancellationToken);

        if (slotsResult.IsFailure)
            return Result.Failure<IReadOnlyList<SlotCandidateDto>>(slotsResult.Error);

        IEnumerable<SlotCandidateDto> candidates = slotsResult.Value;

        // Same pattern as GetNextSessionCandidatesAsync's PreferredDays filter below —
        // narrow the already-ranked list post-fetch, since the recommendation service
        // has no concept of weekday filtering.
        if (preference.DayToken == RelativeDayToken.SpecificWeekdays
            && preference.PreferredWeekdays != DaysOfWeekFlags.None)
        {
            candidates = candidates.Where(c => MatchesPreferredDays(c.Start.DayOfWeek, preference.PreferredWeekdays));
        }

        // Already ranked by Score desc, Start asc — just take the top N for the frontend cards.
        var topSlots = candidates.Take(topN).ToList();

        return Result.Success<IReadOnlyList<SlotCandidateDto>>(topSlots);
    }

    public async Task<Guid?> GetPackageDoctorIdAsync(Guid packageId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PatientSessionPackage>()
            .Where(p => p.Id == packageId)
            .Select(p => (Guid?)p.DoctorId)
            .FirstOrDefaultAsync(cancellationToken);
    }
    public async Task<Result<PatientSessionPackageSummaryDto>> GetPackageSummaryAsync(Guid packageId,
    CancellationToken cancellationToken = default)
    {
        var package = await _context.Set<PatientSessionPackage>()
            .FirstOrDefaultAsync(p => p.Id == packageId, cancellationToken);

        if (package is null)
            return Result.Failure<PatientSessionPackageSummaryDto>(SchedulingErrors.PackageNotFound);

        var patientFreeTimeText = await _context.Set<Patient>()
            .Where(p => p.Id == package.PatientId)
            .Select(p => p.PatientFreeTime)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var nextSessionNumber = Math.Min(package.ScheduledSessions + 1, package.TotalSessions);

        return Result.Success(new PatientSessionPackageSummaryDto
        {
            PackageId = package.Id,
            PatientId = package.PatientId,
            DoctorId = package.DoctorId,
            TotalSessions = package.TotalSessions,
            ScheduledSessions = package.ScheduledSessions,
            RemainingSessions = package.RemainingSessions,
            NextSessionNumber = nextSessionNumber,
            Status = package.Status,
            minimumGapBetweenSessionsDays = package.MinimumGapBetweenSessionsDays,
            SessionsPerWeek = package.SessionsPerWeek,
            SessionDuration = package.SessionDuration,
            PatientFreeTimeText = patientFreeTimeText
        });
    }

    private static SessionBookingRoundDto BuildRound(
    PatientSessionPackage package, int sessionNumber, int weeklyTarget, int scheduledThisWeek,
    DateOnly weekStart, DateOnly weekEnd, bool quotaMet, bool noRoom,
    IReadOnlyList<SlotCandidateDto> candidates, string patientFreeTimeText) => new()
    {
        PackageId = package.Id,
        SessionNumber = sessionNumber,
        TotalSessions = package.TotalSessions,
        RemainingSessions = package.RemainingSessions,
        WeeklyTargetCount = weeklyTarget,
        ScheduledThisWeek = scheduledThisWeek,
        WeekStart = weekStart,
        WeekEnd = weekEnd,
        WeeklyQuotaMet = quotaMet,
        NoRoomLeftThisWeek = noRoom,
        Candidates = candidates,
        PatientFreeTimeText = patientFreeTimeText
    };

    private static bool MatchesPreferredDays(DayOfWeek day, DaysOfWeekFlags flags)
    {
        var flag = day switch
        {
            DayOfWeek.Sunday => DaysOfWeekFlags.Sunday,
            DayOfWeek.Monday => DaysOfWeekFlags.Monday,
            DayOfWeek.Tuesday => DaysOfWeekFlags.Tuesday,
            DayOfWeek.Wednesday => DaysOfWeekFlags.Wednesday,
            DayOfWeek.Thursday => DaysOfWeekFlags.Thursday,
            DayOfWeek.Friday => DaysOfWeekFlags.Friday,
            DayOfWeek.Saturday => DaysOfWeekFlags.Saturday,
            _ => DaysOfWeekFlags.None
        };
        return flags.HasFlag(flag);
    }
    private static (TimeOnly? From, TimeOnly? To) IntersectTimeWindows(
        TimeOnly? aFrom, TimeOnly? aTo, TimeOnly? bFrom, TimeOnly? bTo)
        {
            TimeOnly? from = (aFrom, bFrom) switch
            {
                (null, null) => null,
                (null, _) => bFrom,
                (_, null) => aFrom,
                _ => aFrom!.Value > bFrom!.Value ? aFrom : bFrom
            };

            TimeOnly? to = (aTo, bTo) switch
            {
                (null, null) => null,
                (null, _) => bTo,
                (_, null) => aTo,
                _ => aTo!.Value < bTo!.Value ? aTo : bTo
            };

            return (from, to);
        }

    private static (TimeOnly? From, TimeOnly? To) MapPreferredTimeOfDay(PreferredTimeOfDay preference) =>
        preference switch
        {
            PreferredTimeOfDay.Morning => (new TimeOnly(6, 0), new TimeOnly(12, 0)),
            PreferredTimeOfDay.Afternoon => (new TimeOnly(12, 0), new TimeOnly(17, 0)),
            PreferredTimeOfDay.Evening => (new TimeOnly(17, 0), new TimeOnly(22, 0)),
            _ => (null, null)
        };
}
