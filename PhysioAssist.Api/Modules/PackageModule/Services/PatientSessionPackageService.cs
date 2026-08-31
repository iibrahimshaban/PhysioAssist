using PhysioAssist.Api.Modules.PackageModule.Entities;
using PhysioAssist.Api.Modules.PackageModule.Errors;
using PhysioAssist.Api.Modules.PatientModule.Entities;
using PhysioAssist.Api.Shared.Dtos.Package;
using PhysioAssist.Api.Shared.Interfaces.Exposed;

namespace PhysioAssist.Api.Modules.PackageModule.Services;

public class PatientSessionPackageService(ApplicationDbContext _context) : IPatientSessionPackageService
{
    public async Task<Result<CreateSessionPackageResult>> CreatePackageAsync(
        CreateSessionPackageRequest request,
        CancellationToken cancellationToken = default)
    {
        var package = new PatientSessionPackage
        {
            Id = Guid.CreateVersion7(),
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            TreatmentSchedulePlanId = request.TreatmentSchedulePlanId,
            TotalSessions = request.TotalSessions,
            SessionDuration = request.SessionDuration,
            ScheduledSessions = 0,
            RemainingSessions = request.TotalSessions,
            Status = PackageStatus.Active,
            SessionsPerWeek = request.SessionsPerWeek,
            MinimumGapBetweenSessionsDays = request.MinimumGapBetweenSessionsDays,
            Priority = request.Priority
        };

        _context.PatientSessionPackages.Add(package);
        await _context.SaveChangesAsync(cancellationToken);

        // Package creation never books a slot anymore — that always happens
        // in Scheduling's CreatePackageWithFirstBookingAsync, which calls this
        // method for the package half and then books separately.
        return Result.Success(new CreateSessionPackageResult
        {
            PackageId = package.Id,
            ScheduledSessions = 0
        });
    }

    public async Task<Guid?> GetPackageDoctorIdAsync(Guid packageId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PatientSessionPackage>()
            .Where(p => p.Id == packageId)
            .Select(p => (Guid?)p.DoctorId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Result<PatientSessionPackageSummaryDto>> GetPackageSummaryAsync(
        Guid packageId, CancellationToken cancellationToken = default)
    {
        var package = await _context.Set<PatientSessionPackage>()
            .FirstOrDefaultAsync(p => p.Id == packageId, cancellationToken);

        if (package is null)
            return Result.Failure<PatientSessionPackageSummaryDto>(PatientSessionPackageErrors.NotFound);

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

    public async Task<Result<PatientSessionPackageSummaryDto>> GetPackageSummaryByPlanIdAsync(
        Guid treatmentPlanId, CancellationToken cancellationToken = default)
    {
        var packageId = await _context.Set<PatientSessionPackage>()
            .Where(p => p.TreatmentSchedulePlanId == treatmentPlanId)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (packageId is null)
            return Result.Failure<PatientSessionPackageSummaryDto>(PatientSessionPackageErrors.NotFound);

        return await GetPackageSummaryAsync(packageId.Value, cancellationToken);
    }

    public async Task<Result<PackageSchedulingContextDto>> GetSchedulingContextAsync(
    Guid packageId, CancellationToken cancellationToken = default)
    {
        var package = await _context.Set<PatientSessionPackage>()
            .FirstOrDefaultAsync(p => p.Id == packageId, cancellationToken);

        if (package is null)
            return Result.Failure<PackageSchedulingContextDto>(PatientSessionPackageErrors.NotFound);

        return Result.Success(new PackageSchedulingContextDto
        {
            PackageId = package.Id,
            PatientId = package.PatientId,
            DoctorId = package.DoctorId,
            TotalSessions = package.TotalSessions,
            ScheduledSessions = package.ScheduledSessions,
            RemainingSessions = package.RemainingSessions,
            SessionDuration = package.SessionDuration,
            SessionsPerWeek = package.SessionsPerWeek,
            MinimumGapBetweenSessionsDays = package.MinimumGapBetweenSessionsDays,
            CreatedAt = package.CreatedAt
        });
    }

    public async Task<Result> RecordSessionScheduledAsync(
        Guid packageId, CancellationToken cancellationToken = default)
    {
        var package = await _context.Set<PatientSessionPackage>()
            .FirstOrDefaultAsync(p => p.Id == packageId, cancellationToken);

        if (package is null)
            return Result.Failure(PatientSessionPackageErrors.NotFound);

        package.ScheduledSessions++;
        package.RemainingSessions--;

        if (package.RemainingSessions == 0)
            package.Status = PackageStatus.Completed;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<PatientSessionPackageSummaryDto>> ExtendPackageAsync(
    Guid packageId, ExtendPackageRequest request, Guid actingUserId,
    CancellationToken cancellationToken = default)
    {
        if (request.AdditionalSessions <= 0)
            return Result.Failure<PatientSessionPackageSummaryDto>(PatientSessionPackageErrors.InvalidTotalSessions);

        var package = await _context.Set<PatientSessionPackage>()
            .FirstOrDefaultAsync(p => p.Id == packageId, cancellationToken);

        if (package is null)
            return Result.Failure<PatientSessionPackageSummaryDto>(PatientSessionPackageErrors.NotFound);

        if (package.Status == PackageStatus.Cancelled)
            return Result.Failure<PatientSessionPackageSummaryDto>(PatientSessionPackageErrors.PackageAlreadyStopped);

        package.TotalSessions += request.AdditionalSessions;
        package.RemainingSessions += request.AdditionalSessions;

        if (package.Status == PackageStatus.Completed)
            package.Status = PackageStatus.Active;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetPackageSummaryAsync(packageId, cancellationToken);
    }

    public async Task<Result<PatientSessionPackageSummaryDto>> StopPackageAsync(
        Guid packageId, StopPackageRequest request, Guid actingUserId,
        CancellationToken cancellationToken = default)
    {
        var package = await _context.Set<PatientSessionPackage>()
            .FirstOrDefaultAsync(p => p.Id == packageId, cancellationToken);

        if (package is null)
            return Result.Failure<PatientSessionPackageSummaryDto>(PatientSessionPackageErrors.NotFound);

        if (package.Status == PackageStatus.Cancelled)
            return Result.Failure<PatientSessionPackageSummaryDto>(PatientSessionPackageErrors.PackageAlreadyStopped);

        package.Status = PackageStatus.Cancelled;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetPackageSummaryAsync(packageId, cancellationToken);
    }

    public async Task<Result> SubstituteDoctorForSessionAsync(
        Guid packageId, SubstituteDoctorForSessionRequest request, Guid actingUserId,
        CancellationToken cancellationToken = default)
    {
        var package = await _context.Set<PatientSessionPackage>()
            .FirstOrDefaultAsync(p => p.Id == packageId, cancellationToken);

        if (package is null)
            return Result.Failure(PatientSessionPackageErrors.NotFound);

        // NOTE: does not touch package.DoctorId, ScheduleSlot.DoctorId, or Session.DoctorId
        // here — actually reassigning the session's doctor is a SessionModule/Scheduling
        // concern (Session.DoctorId), out of PackageModule's boundary. This method only
        // records the audit entry. The caller (controller) is responsible for calling
        // whatever SessionModule exposes to actually flip Session.DoctorId, OR this needs
        // a cross-module call the same way Scheduling calls into PackageModule.
        var history = new PackageDoctorAssignmentHistory
        {
            UniqueId = Guid.CreateVersion7(),
            PackageId = packageId,
            OldDoctorId = package.DoctorId,
            NewDoctorId = request.NewDoctorId,
            ChangeType = PackageDoctorAssignmentChangeType.SingleSessionSubstitution,
            SessionId = request.SessionId,
            ChangedAt = DateTime.UtcNow,
            Reason = request.Reason,
            ChangedByUserId = actingUserId
        };

        _context.Set<PackageDoctorAssignmentHistory>().Add(history);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ReassignPackageDoctorAsync(
        Guid packageId, ReassignPackageDoctorRequest request, Guid actingUserId,
        CancellationToken cancellationToken = default)
    {
        var package = await _context.Set<PatientSessionPackage>()
            .FirstOrDefaultAsync(p => p.Id == packageId, cancellationToken);

        if (package is null)
            return Result.Failure(PatientSessionPackageErrors.NotFound);

        var oldDoctorId = package.DoctorId;
        package.DoctorId = request.NewDoctorId;   // full reassignment DOES change the package's doctor

        var history = new PackageDoctorAssignmentHistory
        {
            UniqueId = Guid.CreateVersion7(),
            PackageId = packageId,
            OldDoctorId = oldDoctorId,
            NewDoctorId = request.NewDoctorId,
            ChangeType = PackageDoctorAssignmentChangeType.FullPackageReassignment,
            SessionId = null,
            ChangedAt = DateTime.UtcNow,
            Reason = request.Reason,
            ChangedByUserId = actingUserId
        };

        _context.Set<PackageDoctorAssignmentHistory>().Add(history);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<PackageDoctorAssignmentHistoryDto>>> GetAssignmentHistoryAsync(
        Guid packageId, CancellationToken cancellationToken = default)
    {
        var history = await _context.Set<PackageDoctorAssignmentHistory>()
            .Where(h => h.PackageId == packageId)
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new PackageDoctorAssignmentHistoryDto
            {
                UniqueId = h.UniqueId,
                PackageId = h.PackageId,
                OldDoctorId = h.OldDoctorId,
                NewDoctorId = h.NewDoctorId,
                ChangeType = h.ChangeType.ToString(),
                SessionId = h.SessionId,
                ChangedAt = h.ChangedAt,
                Reason = h.Reason,
                ChangedByUserId = h.ChangedByUserId
            })
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PackageDoctorAssignmentHistoryDto>>(history);
    }
    public async Task<Result<IReadOnlyList<PatientSessionPackageSummaryDto>>> GetTerminalPackagesForPatientAsync(
    Guid patientId, CancellationToken cancellationToken = default)
    {
        var packages = await _context.Set<PatientSessionPackage>()
            .Where(p => p.PatientId == patientId &&
                        (p.Status == PackageStatus.Completed || p.Status == PackageStatus.Cancelled))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        var summaries = packages.Select(p => new PatientSessionPackageSummaryDto
        {
            PackageId = p.Id,
            PatientId = p.PatientId,
            DoctorId = p.DoctorId,
            TotalSessions = p.TotalSessions,
            ScheduledSessions = p.ScheduledSessions,
            RemainingSessions = p.RemainingSessions,
            NextSessionNumber = Math.Min(p.ScheduledSessions + 1, p.TotalSessions),
            Status = p.Status,
            minimumGapBetweenSessionsDays = p.MinimumGapBetweenSessionsDays,
            SessionsPerWeek = p.SessionsPerWeek,
            SessionDuration = p.SessionDuration,
            PatientFreeTimeText = string.Empty   // not relevant for history — historical package
        }).ToList();

        return Result.Success<IReadOnlyList<PatientSessionPackageSummaryDto>>(summaries);
    }
}