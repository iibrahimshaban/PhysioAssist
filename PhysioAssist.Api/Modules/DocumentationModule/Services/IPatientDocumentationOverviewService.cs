using PhysioAssist.Api.Modules.DocumentationModule.Contracts;

namespace PhysioAssist.Api.Modules.DocumentationModule.Services;

public interface IPatientDocumentationOverviewService
{
    Task<List<PatientDocumentationSessionResponse>> GetSessionsAsync(
        Guid doctorId, Guid patientId, CancellationToken ct = default);
}
