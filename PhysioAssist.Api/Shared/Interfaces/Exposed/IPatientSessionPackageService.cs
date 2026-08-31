using PhysioAssist.Api.Shared.Dtos.Package;

namespace PhysioAssist.Api.Shared.Interfaces.Exposed;

public interface IPatientSessionPackageService
{
    Task<Result<CreateSessionPackageResult>> CreatePackageAsync(
        CreateSessionPackageRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<PatientSessionPackageSummaryDto>> GetPackageSummaryAsync(
        Guid packageId, CancellationToken cancellationToken = default);

    Task<Result<PatientSessionPackageSummaryDto>> GetPackageSummaryByPlanIdAsync(
        Guid treatmentPlanId, CancellationToken cancellationToken = default);

    Task<Guid?> GetPackageDoctorIdAsync(
        Guid packageId, CancellationToken cancellationToken = default);

    Task<Result> RecordSessionScheduledAsync(
        Guid packageId, CancellationToken cancellationToken = default);
    Task<Result<PackageSchedulingContextDto>> GetSchedulingContextAsync(
        Guid packageId, CancellationToken cancellationToken = default);

    Task<Result<PatientSessionPackageSummaryDto>> ExtendPackageAsync(
        Guid packageId, ExtendPackageRequest request, Guid actingUserId,
        CancellationToken cancellationToken = default);

    Task<Result<PatientSessionPackageSummaryDto>> StopPackageAsync(
        Guid packageId, StopPackageRequest request, Guid actingUserId,
        CancellationToken cancellationToken = default);
    Task<Result> SubstituteDoctorForSessionAsync(
        Guid packageId, SubstituteDoctorForSessionRequest request, Guid actingUserId,
        CancellationToken cancellationToken = default);
    Task<Result> ReassignPackageDoctorAsync(
        Guid packageId, ReassignPackageDoctorRequest request, Guid actingUserId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PackageDoctorAssignmentHistoryDto>>> GetAssignmentHistoryAsync(
        Guid packageId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PatientSessionPackageSummaryDto>>> GetTerminalPackagesForPatientAsync(
    Guid patientId, CancellationToken cancellationToken = default);
}
