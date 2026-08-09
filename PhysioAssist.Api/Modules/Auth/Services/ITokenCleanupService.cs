namespace PhysioAssist.Api.Modules.Auth.Services;

public interface ITokenCleanupService
{
    Task PurgeExpiredAndRevokedAsync(CancellationToken cancellationToken = default);
}
