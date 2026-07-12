using PhysioAssist.Api.Persistence;

namespace PhysioAssist.Api.Modules.SessionModule.Services;

public class SessionSummaryWriter(ApplicationDbContext context) : ISessionSummaryWriter
{
    public async Task<bool> SaveSummaryAsync(Guid sessionId, string summaryText, CancellationToken ct = default)
    {
        var session = await context.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session is null)
            return false;

        session.SummaryText = summaryText;
        session.SummaryGeneratedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return true;
    }
}
