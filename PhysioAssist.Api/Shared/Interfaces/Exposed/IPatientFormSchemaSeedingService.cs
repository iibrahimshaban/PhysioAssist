namespace PhysioAssist.Api.Shared.Interfaces.Exposed;

public interface IPatientFormSchemaSeedingService
{
    Task<Result> SeedDefaultSchemaAsync(Guid clinicId, string clinicName, CancellationToken cancellationToken = default);
}
