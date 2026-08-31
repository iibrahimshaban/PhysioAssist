namespace PhysioAssist.Api.Shared.Interfaces.Exposed;

public interface ISessionAssignmentService
{
    Task<Result> UpdateSessionDoctorAsync(
        Guid sessionId, Guid newDoctorId, CancellationToken cancellationToken = default);
}
