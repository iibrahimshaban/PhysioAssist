using PhysioAssist.Api.Shared.Dtos.Patient;

namespace PhysioAssist.Api.Shared.Interfaces;

public interface IPatientQueryService
{
    Task<List<PatientLookupResult>> FindByNameAsync(string namePart, CancellationToken ct = default);
    Task<PatientCategory?> GetPatientCategoryAsync(Guid doctorId, Guid patientId, CancellationToken ct = default);
    Task<Result<PatientResponse>> GetPatientAsync(Guid patientId, CancellationToken ct = default);
}
