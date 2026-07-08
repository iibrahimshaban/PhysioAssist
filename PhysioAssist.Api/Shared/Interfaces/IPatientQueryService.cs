using PhysioAssist.Api.Shared.Dtos.Patient;

namespace PhysioAssist.Api.Shared.Interfaces;

public interface IPatientQueryService
{
    Task<List<PatientLookupResult>> FindByNameAsync(string namePart, CancellationToken ct = default);
}
