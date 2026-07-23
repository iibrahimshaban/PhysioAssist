using PhysioAssist.Api.Shared.Dtos.Patient;

namespace PhysioAssist.Api.Shared.Interfaces.Exposed;

public interface IPatientQueryService
{
    Task<List<PatientLookupResult>> FindByNameAsync(string namePart, CancellationToken ct = default);
    Task<PatientCategory?> GetPatientCategoryAsync(Guid doctorId, Guid patientId, CancellationToken ct = default);
    Task<Result<PatientResponse>> GetPatientAsync(Guid patientId, CancellationToken ct = default);
    Task<Result<List<PatientResponse>>> GetAllPatientsForDoctorAsync( Guid DoctorId, CancellationToken ct = default);
    Task<Result<Guid>> CreatePatientFromIntakeAsync(CreatePatientFromIntakeRequest request,
        CancellationToken cancellationToken = default);
    Task<Result<PatientTimePreferenceInfo>> GetPatientTimePreferenceAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<Result<PatientTimePreferenceInfo>> ResolvePatientTimePreferenceAsync(Guid patientId,string? freeTimeOverrideText,
        bool persistOverride, CancellationToken cancellationToken = default);
}
