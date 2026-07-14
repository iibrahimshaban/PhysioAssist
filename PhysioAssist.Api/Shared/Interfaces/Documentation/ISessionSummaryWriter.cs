namespace PhysioAssist.Api.Shared.Interfaces.Documentation;

public interface ISessionSummaryWriter
{
    Task<bool> SaveSummaryAsync(Guid sessionId, string summaryText, CancellationToken ct = default);
}
