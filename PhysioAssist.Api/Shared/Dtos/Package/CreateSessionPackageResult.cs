using PhysioAssist.Api.Shared.Dtos.Schedule;

namespace PhysioAssist.Api.Shared.Dtos.Package;

public class CreateSessionPackageResult
{
    public required Guid PackageId { get; init; }
    public required int ScheduledSessions { get; init; }
}
