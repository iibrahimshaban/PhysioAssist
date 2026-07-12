namespace PhysioAssist.Api.Shared.Interfaces;

public interface IPatientFormSchemaSeedingService
{
    Task<Result> SeedDefaultSchemaAsync(Guid doctorId, string clinicName, CancellationToken cancellationToken = default);
}
