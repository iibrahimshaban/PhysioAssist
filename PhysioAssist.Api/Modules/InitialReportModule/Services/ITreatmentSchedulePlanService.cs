using PhysioAssist.Api.Modules.InitialReportModule.DTOs;
using PhysioAssist.Api.Shared.Dtos.Schedule;

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
    Task<Result<PatientSchedulingContextDto>> GetSchedulingContextForPatientAsync(
    Guid patientId, CancellationToken cancellationToken = default);

    Task<Result<PatientSessionPackageSummaryDto>> ConvertPlanToPackageAsync(
        Guid treatmentPlanId, ConvertPlanToPackageRequest request, CancellationToken cancellationToken = default);
    Task<Guid?> GetPlanDoctorIdAsync(Guid treatmentPlanId, CancellationToken cancellationToken = default);
}
