namespace PhysioAssist.Api.Modules.Intake.Services;

public interface IIntakeService
{
    Task<Result> EnsureSchemaBelongsToDoctorAsync(Guid schemaId, Guid doctorId, CancellationToken cancellationToken = default);
    Task<Result> EnsureIntakeBelongsToDoctorAsync(Guid intakeId, Guid doctorId, CancellationToken cancellationToken = default);
}
