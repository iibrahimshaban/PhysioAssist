namespace PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;

public interface IPatientSessionPackageAdjustmentService
{
    Task<Result> ExtendByOneSessionAsync(Guid packageId, CancellationToken cancellationToken = default);
}
