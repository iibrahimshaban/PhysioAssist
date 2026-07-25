using PhysioAssist.Api.Shared.Dtos.Intake;

namespace PhysioAssist.Api.Shared.Interfaces.Exposed
{
    public interface IPatientOverviewIntakeQueryService
    {
        Task<Result<PatientOverviewIntakeResult>> GetOverviewDataForPatientAsync(Guid patientId, CancellationToken ct = default);
    }
}
