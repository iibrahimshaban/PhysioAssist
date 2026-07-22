using PhysioAssist.Api.Modules.InitialReportModule.DTOs;
using PhysioAssist.Api.Modules.InitialReportModule.Entities;
using PhysioAssist.Api.Modules.InitialReportModule.Errors;
using PhysioAssist.Api.Modules.InitialReportModule.Repositories;
using PhysioAssist.Api.Modules.Scheduling.DTO.AgentDtos;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;
using PhysioAssist.Api.Shared.Dtos.Schedule;

namespace PhysioAssist.Api.Modules.InitialReportModule.Services;

public class TreatmentSchedulePlanService(
        ITreatmentSchedulePlanRepository _planRepository,
        IInitialReportRepository _reportRepository,
        IIntakeQueryService _intakeQueryService,
        IPatientSlotRecommendationService _slotRecommendationService,
        IScheduleSlotQueryService _packageService,
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
            // Already booked or handed to the receptionist — editing now would
            // silently invalidate a decision that's already been acted on.
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

        var packageResult = await _packageService.CreatePackageWithFirstBookingAsync(new CreatePackageWithFirstBookingRequest
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

        // Deferred — the receptionist's own search flow (built separately) will
        // recompute candidates when she actually works this.
        return Result.Success(MapToResponse(plan, []));
    }

    private async Task<IReadOnlyList<SlotCandidateDto>> GetCandidateSlotsAsync(
        TreatmentSchedulePlan plan, Guid doctorId, Guid patientId, CancellationToken cancellationToken)
    {
        if (plan.Status != TreatmentSchedulePlanStatus.Pending)
            return [];

        var freeTimeResult = await _intakeQueryService.GetPatientFreeTimeTextAsync(patientId, cancellationToken);
        var freeTimeText = freeTimeResult.IsSuccess ? freeTimeResult.Value : null;

        var slotsResult = await _slotRecommendationService.GetTopRecommendedSlotsAsync(
            doctorId,
            TimeSpan.FromMinutes(plan.SessionDurationMinutes),
            freeTimeText ?? string.Empty, // empty -> "no preference", handled by the pipeline already
            DefaultCandidateCount,
            cancellationToken);

        // A recommendation-lookup failure shouldn't block the doctor from seeing/saving
        // the plan itself — just show no candidates this time rather than erroring the whole request.
        return slotsResult.IsSuccess ? slotsResult.Value : [];
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
