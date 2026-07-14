namespace PhysioAssist.Api.Shared.Interfaces.Exposed;

public interface IPatientFormSchemaSeedingService
{
    Task<Result> SeedDefaultSchemaAsync(Guid doctorId, string clinicName, CancellationToken cancellationToken = default);
}
