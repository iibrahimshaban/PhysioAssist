namespace PhysioAssist.Api.Shared.Interfaces;

public interface ISessionSummaryWriter
{
    Task<bool> SaveSummaryAsync(Guid sessionId, string summaryText, CancellationToken ct = default);
}
