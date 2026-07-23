using PhysioAssist.Api.Modules.InitialReportModule.DTOs;
using PhysioAssist.Api.Modules.InitialReportModule.Entities;
using PhysioAssist.Api.Modules.InitialReportModule.Errors;
using PhysioAssist.Api.Modules.InitialReportModule.Repositories;
using PhysioAssist.Api.Shared.Dtos.Schedule;

namespace PhysioAssist.Api.Modules.InitialReportModule.Services;

public class TreatmentSchedulePlanService(
        ITreatmentSchedulePlanRepository _planRepository,
        IInitialReportRepository _reportRepository,
        IPatientSessionSchedulingService _PatientSessionSchedulingService,
        IUnitOfWork _unitOfWork) : ITreatmentSchedulePlanService
{

    private const int DefaultCandidateCount = 5;

    public async Task<Result<TreatmentSchedulePlanResponse>> UpsertAsync(
        Guid reportId, UpsertTreatmentSchedulePlanRequest request, CancellationToken cancellationToken = default)
    {
        if (request.TotalSessions <= 0)
            return Result.Failure<TreatmentSchedulePlanResponse>(TreatmentSchedulePlanErrors.InvalidTotalSessions);

        if (request.SessionDurationMinutes <= 0)
            return Result.Failure<TreatmentSchedulePlanResponse>(TreatmentSchedulePlanErrors.InvalidSessionDuration);

        var report = await _reportRepository.GetByIdAsync(reportId);
        if (report is null)
            return Result.Failure<TreatmentSchedulePlanResponse>(TreatmentSchedulePlanErrors.NotFound);

        var plan = await _planRepository.GetByReportIdAsync(reportId, cancellationToken);

        if (plan is null)
        {
            plan = new TreatmentSchedulePlan { ReportId = reportId };
            await _planRepository.AddAsync(plan, cancellationToken);
        }
        else if (plan.Status != TreatmentSchedulePlanStatus.Pending)
        {
            return Result.Failure<TreatmentSchedulePlanResponse>(TreatmentSchedulePlanErrors.AlreadyResolved);
        }
        else
        {
            _planRepository.Update(plan);
        }

        plan.TotalSessions = request.TotalSessions;
        plan.SessionDurationMinutes = request.SessionDurationMinutes;
        plan.SessionsPerWeek = request.SessionsPerWeek;
        plan.MinimumGapBetweenSessionsDays = request.MinimumGapBetweenSessionsDays;
        plan.PreferredTimeOfDay = request.PreferredTimeOfDay;
        plan.PreferredDays = request.PreferredDays;
        plan.Priority = request.Priority;

        await _unitOfWork.SaveAsync(cancellationToken);

        var candidates = await GetCandidateSlotsAsync(plan, report.DoctorId, report.PatientId, cancellationToken);
        return Result.Success(MapToResponse(plan, candidates));
    }

    public async Task<Result<TreatmentSchedulePlanResponse>> GetAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        var report = await _reportRepository.GetByIdAsync(reportId);
        if (report is null)
            return Result.Failure<TreatmentSchedulePlanResponse>(TreatmentSchedulePlanErrors.NotFound);

        var plan = await _planRepository.GetByReportIdAsync(reportId, cancellationToken);
        if (plan is null)
            return Result.Failure<TreatmentSchedulePlanResponse>(TreatmentSchedulePlanErrors.NotFound);

        var candidates = await GetCandidateSlotsAsync(plan, report.DoctorId, report.PatientId, cancellationToken);
        return Result.Success(MapToResponse(plan, candidates));
    }

    public async Task<Result<TreatmentSchedulePlanResponse>> BookNowAsync(
        Guid reportId, BookTreatmentSlotRequest request, CancellationToken cancellationToken = default)
    {
        var report = await _reportRepository.GetByIdAsync(reportId);
        if (report is null)
            return Result.Failure<TreatmentSchedulePlanResponse>(TreatmentSchedulePlanErrors.NotFound);

        var plan = await _planRepository.GetByReportIdAsync(reportId, cancellationToken);
        if (plan is null)
            return Result.Failure<TreatmentSchedulePlanResponse>(TreatmentSchedulePlanErrors.NotFound);

        if (plan.Status != TreatmentSchedulePlanStatus.Pending)
            return Result.Failure<TreatmentSchedulePlanResponse>(TreatmentSchedulePlanErrors.AlreadyResolved);

        var packageResult = await _PatientSessionSchedulingService.CreatePackageWithFirstBookingAsync(new CreatePackageWithFirstBookingRequest
        {
            PatientId = report.PatientId,
            DoctorId = report.DoctorId,
            TotalSessions = plan.TotalSessions,
            SessionDuration = TimeSpan.FromMinutes(plan.SessionDurationMinutes),
            SessionsPerWeek = plan.SessionsPerWeek,
            MinimumGapBetweenSessionsDays = plan.MinimumGapBetweenSessionsDays,
            PreferredTimeOfDay = plan.PreferredTimeOfDay,
            PreferredDays = plan.PreferredDays,
            Priority = plan.Priority,
            SlotStart = request.SlotStart,
            SlotEnd = request.SlotEnd
        }, cancellationToken);

        if (packageResult.IsFailure)
            return Result.Failure<TreatmentSchedulePlanResponse>(packageResult.Error);

        plan.Status = TreatmentSchedulePlanStatus.Booked;
        plan.PackageId = packageResult.Value.Id;

        _planRepository.Update(plan);
        await _unitOfWork.SaveAsync(cancellationToken);

        // Booked — nothing left to recommend.
        return Result.Success(MapToResponse(plan, []));
    }

    public async Task<Result<TreatmentSchedulePlanResponse>> SendToReceptionistAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        var plan = await _planRepository.GetByReportIdAsync(reportId, cancellationToken);
        if (plan is null)
            return Result.Failure<TreatmentSchedulePlanResponse>(TreatmentSchedulePlanErrors.NotFound);

        if (plan.Status != TreatmentSchedulePlanStatus.Pending)
            return Result.Failure<TreatmentSchedulePlanResponse>(TreatmentSchedulePlanErrors.AlreadyResolved);

        plan.Status = TreatmentSchedulePlanStatus.SentToReceptionist;

        _planRepository.Update(plan);
        await _unitOfWork.SaveAsync(cancellationToken);

        return Result.Success(MapToResponse(plan, []));
    }

    private async Task<IReadOnlyList<SlotCandidateDto>> GetCandidateSlotsAsync(
    TreatmentSchedulePlan plan, Guid doctorId, Guid patientId, CancellationToken cancellationToken)
    {
        if (plan.Status != TreatmentSchedulePlanStatus.Pending)
            return [];

        var slotsResult = await _PatientSessionSchedulingService.GetTopRecommendedSlotsAsync(
            doctorId,
            TimeSpan.FromMinutes(plan.SessionDurationMinutes),
            patientId,
            DefaultCandidateCount,
            cancellationToken);

        return slotsResult.IsSuccess ? slotsResult.Value : [];
    }
    public async Task<Result<PatientSchedulingContextDto>> GetSchedulingContextForPatientAsync(
    Guid patientId, CancellationToken cancellationToken = default)
    {
        var report = await _reportRepository.GetByPatientIdAsync(patientId);
        if (report is null)
            return Result.Success(new PatientSchedulingContextDto { State = PatientSchedulingState.NoInitialReport });

        var plan = await _planRepository.GetByReportIdAsync(report.FirstOrDefault()!.Id, cancellationToken);
        if (plan is null || plan.Status == TreatmentSchedulePlanStatus.Pending)
            return Result.Success(new PatientSchedulingContextDto { State = PatientSchedulingState.PlanPending });

        if (plan.Status == TreatmentSchedulePlanStatus.SentToReceptionist)
        {
            return Result.Success(new PatientSchedulingContextDto
            {
                State = PatientSchedulingState.ReadyToSchedule,
                PendingPlan = new PendingTreatmentPlanDto
                {
                    TreatmentPlanId = plan.Id,
                    ReportId = plan.ReportId,
                    TotalSessions = plan.TotalSessions,
                    SessionDurationMinutes = plan.SessionDurationMinutes,
                    SessionsPerWeek = plan.SessionsPerWeek,
                    MinimumGapBetweenSessionsDays = plan.MinimumGapBetweenSessionsDays,
                    PreferredTimeOfDay = plan.PreferredTimeOfDay,
                    PreferredDays = plan.PreferredDays,
                    Priority = plan.Priority
                }
            });
        }

        // Status == Booked from here on.
        if (plan.PackageId is null)
            // Shouldn't happen — Booked should always carry a PackageId — but fail
            // safe instead of crashing the receptionist's screen on bad data.
            return Result.Failure<PatientSchedulingContextDto>(TreatmentSchedulePlanErrors.NotFound);

        var summaryResult = await _PatientSessionSchedulingService.GetPackageSummaryAsync(plan.PackageId.Value, cancellationToken);
        if (summaryResult.IsFailure)
            return Result.Failure<PatientSchedulingContextDto>(summaryResult.Error);

        return Result.Success(new PatientSchedulingContextDto
        {
            State = PatientSchedulingState.ActivePackage,
            ActivePackage = summaryResult.Value
        });
    }

    public async Task<Result<PatientSessionPackageSummaryDto>> ConvertPlanToPackageAsync(
        Guid treatmentPlanId, ConvertPlanToPackageRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await _planRepository.GetByIdAsync(treatmentPlanId);

        if (plan is null)
            return Result.Failure<PatientSessionPackageSummaryDto>(TreatmentSchedulePlanErrors.NotFound);

        if (plan.Status != TreatmentSchedulePlanStatus.SentToReceptionist)
            return Result.Failure<PatientSessionPackageSummaryDto>(TreatmentSchedulePlanErrors.AlreadyResolved);

        var report = await _reportRepository.GetByIdAsync(plan.ReportId);
        if (report is null)
            return Result.Failure<PatientSessionPackageSummaryDto>(TreatmentSchedulePlanErrors.NotFound);

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var packageResult = await _PatientSessionSchedulingService.CreatePackageAsync(new CreateSessionPackageRequest
        {
            PatientId = report.PatientId,
            DoctorId = report.DoctorId,
            TotalSessions = plan.TotalSessions,
            SessionDuration = TimeSpan.FromMinutes(plan.SessionDurationMinutes),
            SessionsPerWeek = request.SessionsPerWeek ?? plan.SessionsPerWeek,
            MinimumGapBetweenSessionsDays = request.MinimumGapBetweenSessionsDays ?? plan.MinimumGapBetweenSessionsDays,
            PreferredTimeOfDay = request.PreferredTimeOfDay ?? plan.PreferredTimeOfDay,
            PreferredDays = request.PreferredDays ?? plan.PreferredDays,
            Priority = request.Priority ?? plan.Priority,
            FirstSessionSlot = null
        }, cancellationToken);

        if (packageResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<PatientSessionPackageSummaryDto>(packageResult.Error);
        }

        plan.Status = TreatmentSchedulePlanStatus.Booked;
        plan.PackageId = packageResult.Value.PackageId;

        _planRepository.Update(plan);
        await _unitOfWork.SaveAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var summaryResult = await _PatientSessionSchedulingService.GetPackageSummaryAsync(packageResult.Value.PackageId, cancellationToken);
        return summaryResult;
    }
    public async Task<Guid?> GetPlanDoctorIdAsync(Guid treatmentPlanId, CancellationToken cancellationToken = default)
    {
        var plan = await _planRepository.GetByIdAsync(treatmentPlanId);
        if (plan is null)
            return null;

        var report = await _reportRepository.GetByIdAsync(plan.ReportId);
        return report?.DoctorId;
    }

    private static TreatmentSchedulePlanResponse MapToResponse(TreatmentSchedulePlan plan, IReadOnlyList<SlotCandidateDto> candidates) => new(
        plan.Id,
        plan.ReportId,
        plan.TotalSessions,
        plan.SessionDurationMinutes,
        plan.SessionsPerWeek,
        plan.MinimumGapBetweenSessionsDays,
        plan.PreferredTimeOfDay,
        plan.PreferredDays,
        plan.Priority,
        plan.Status,
        plan.PackageId,
        candidates);
}
