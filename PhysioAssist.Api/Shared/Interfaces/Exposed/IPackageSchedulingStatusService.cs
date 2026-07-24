using PhysioAssist.Api.Shared.Dtos.Session;

namespace PhysioAssist.Api.Shared.Interfaces.Exposed;

public interface IPackageSchedulingStatusService
{
    Task<Result<NextSessionBookingContextDto>> GetContextAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
