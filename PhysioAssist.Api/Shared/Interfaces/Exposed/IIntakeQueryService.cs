using PhysioAssist.Api.Shared.Dtos.Intake;

namespace PhysioAssist.Api.Shared.Interfaces.Exposed;

public interface IIntakeQueryService
{
    Task<Result<PreVisitIntakeDataResponse>> GetPreVisitIntakeByPatientIdAsync(Guid patientId);
    Task<Result<PatientIntakeSummaryResponse>> GetPatientIntakeSummaryAsync(Guid patientId);
    Task<Result<string?>> GetPatientFreeTimeTextAsync(Guid patientId, CancellationToken cancellationToken = default);
}
