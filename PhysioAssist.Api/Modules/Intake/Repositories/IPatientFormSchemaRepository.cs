using PhysioAssist.Api.Modules.Intake.Entities;

namespace PhysioAssist.Api.Modules.Intake.Repositories;

public interface IPatientFormSchemaRepository
{
    Task AddAsync(PatientFormSchema schema, CancellationToken cancellationToken = default);
    Task<PatientFormSchema?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PatientFormSchema?> GetPublishedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PatientFormSchema?> GetDefaultForClinicAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientFormSchema>> GetByClinicAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<bool> ExistsNameForClinicAsync(Guid clinicId, string name, Guid? excludeId, CancellationToken cancellationToken = default);
    Task UnsetDefaultSchemasAsync(Guid clinicId, CancellationToken cancellationToken = default);
    void Update(PatientFormSchema schema);
    void Remove(PatientFormSchema schema);
    Task<IReadOnlyList<PatientFormSchema>> GetCopiesByOriginalFormIdAsync(Guid originalFormId, Guid clinicId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientFormSchema>> GetAllAsync(CancellationToken cancellationToken = default);
}
