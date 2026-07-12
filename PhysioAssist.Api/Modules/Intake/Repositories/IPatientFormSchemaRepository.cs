using PhysioAssist.Api.Modules.Intake.Entities;

namespace PhysioAssist.Api.Modules.Intake.Repositories;

public interface IPatientFormSchemaRepository
{
    Task AddAsync(PatientFormSchema schema, CancellationToken cancellationToken = default);
    Task<PatientFormSchema?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PatientFormSchema?> GetPublishedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PatientFormSchema?> GetDefaultForDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientFormSchema>> GetByDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default);
    Task<bool> ExistsNameForDoctorAsync(Guid doctorId, string name, Guid? excludeId, CancellationToken cancellationToken = default);
    Task UnsetDefaultSchemasAsync(Guid doctorId, CancellationToken cancellationToken = default);
    void Update(PatientFormSchema schema);
}
