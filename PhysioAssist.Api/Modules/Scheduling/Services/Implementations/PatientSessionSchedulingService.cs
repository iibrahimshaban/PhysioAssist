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

        // Resolved once up front so every BuildRound call below — including the
        // early-return branches — can hand it back without a second query.
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

        var packageAnchor = DateOnly.FromDateTime(package.CreatedAt.Add(EgyptOffset));
        var cycleIndex = (today.DayNumber - packageAnchor.DayNumber) / 7;
        var weekStart = packageAnchor.AddDays(cycleIndex * 7);
        var weekEnd = WorkingWeekBoundaryHelper.GetCycleEnd(weekStart, workingDays);

        var weeklyTargetCount = Math.Min(package.SessionsPerWeek, package.RemainingSessions);

        var scheduledThisWeek = weekEnd < weekStart
            ? 0
            : await _context.Set<ScheduleSlot>()
                .Where(s => s.PackageId == packageId
                            && s.Status != SlotStatus.Cancelled
                            && s.SlotStart >= new DateTimeOffset(weekStart.ToDateTime(TimeOnly.MinValue), EgyptOffset)
                            && s.SlotStart <= new DateTimeOffset(weekEnd.ToDateTime(TimeOnly.MaxValue), EgyptOffset))
                .CountAsync(cancellationToken);

        var sessionNumber = package.ScheduledSessions + 1;

        if (scheduledThisWeek >= weeklyTargetCount)
            return Result.Success(BuildRound(package, sessionNumber, weeklyTargetCount, scheduledThisWeek,
                weekStart, weekEnd, quotaMet: true, noRoom: false, candidates: [], patientFreeTimeText));

        if (weekEnd < weekStart)
            return Result.Success(BuildRound(package, sessionNumber, weeklyTargetCount, scheduledThisWeek,
                weekStart, weekStart, quotaMet: false, noRoom: true, candidates: [], patientFreeTimeText));

        var lastConfirmed = await _context.Set<ScheduleSlot>()
            .Where(s => s.PackageId == packageId && s.Status != SlotStatus.Cancelled)
            .OrderByDescending(s => s.SlotStart)
            .Select(s => (DateOnly?)DateOnly.FromDateTime(s.SlotStart.ToOffset(EgyptOffset).Date))
            .FirstOrDefaultAsync(cancellationToken);

        var minDate = lastConfirmed.HasValue
            ? lastConfirmed.Value.AddDays(package.MinimumGapBetweenSessionsDays)
            : today;

        if (minDate < weekStart)
            minDate = weekStart;

        if (minDate > weekEnd)
            return Result.Success(BuildRound(package, sessionNumber, weeklyTargetCount, scheduledThisWeek,
                weekStart, weekEnd, quotaMet: false, noRoom: true, candidates: [], patientFreeTimeText));

        var (packageFrom, packageTo) = MapPreferredTimeOfDay(package.PreferredTimeOfDay);

        TimeOnly? patientFrom = null;
        TimeOnly? patientTo = null;

        // Uses the override if one was sent this round; otherwise falls back to
        // whatever's persisted on the Patient record — same as before this change.
        var patientPreferenceResult = await _patientQueryService.ResolvePatientTimePreferenceAsync(
            package.PatientId, patientFreeTimeOverride, persistFreeTimeOverride, cancellationToken);

        if (patientPreferenceResult.IsSuccess)
        {
            patientFrom = patientPreferenceResult.Value.PreferredTimeFrom;
            patientTo = patientPreferenceResult.Value.PreferredTimeTo;
        }
        // On failure we deliberately don't fail the whole round — just fall back to
        // the package's own window alone, same "don't block booking over a lookup
        // hiccup" philosophy used elsewhere in this service.

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
