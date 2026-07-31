namespace PhysioAssist.Api.Modules.Auth.Services;

public class TokenCleanupService(ApplicationDbContext context, ILogger<TokenCleanupService> logger) : ITokenCleanupService
{
    private static readonly TimeSpan RevokedRetentionPeriod = TimeSpan.FromDays(3);

    public async Task PurgeExpiredAndRevokedAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow;
        var revokedCutoff = cutoff - RevokedRetentionPeriod;

        var deleted = await context.RefreshTokens
            .Where(t => t.ExpiresOn < cutoff || (t.RevokedOn != null && t.RevokedOn < revokedCutoff))
            .ExecuteDeleteAsync(cancellationToken);

        logger.LogInformation("Purged {Count} expired/stale refresh tokens", deleted);
    }
}
