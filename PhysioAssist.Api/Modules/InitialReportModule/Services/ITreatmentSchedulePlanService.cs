using PhysioAssist.Api.Modules.InitialReportModule.DTOs;

namespace PhysioAssist.Api.Modules.InitialReportModule.Services;

public interface ITreatmentSchedulePlanService
{
    Task<Result<TreatmentSchedulePlanResponse>> UpsertAsync(
           Guid reportId, UpsertTreatmentSchedulePlanRequest request, CancellationToken cancellationToken = default);

    Task<Result<TreatmentSchedulePlanResponse>> GetAsync(Guid reportId, CancellationToken cancellationToken = default);

    /// <summary>Doctor picked one of CandidateSlots himself — books it now.</summary>
    Task<Result<TreatmentSchedulePlanResponse>> BookNowAsync(
        Guid reportId, BookTreatmentSlotRequest request, CancellationToken cancellationToken = default);

    /// <summary>Doctor defers — receptionist will collect free time and book later (built separately).</summary>
    Task<Result<TreatmentSchedulePlanResponse>> SendToReceptionistAsync(Guid reportId, CancellationToken cancellationToken = default);
}
