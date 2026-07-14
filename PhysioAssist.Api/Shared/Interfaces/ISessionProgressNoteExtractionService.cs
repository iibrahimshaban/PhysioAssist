using PhysioAssist.Api.Shared.Dtos.Documentation;

namespace PhysioAssist.Api.Shared.Interfaces;

public interface ISessionProgressNoteExtractionService
{
    Task<Result<SessionProgressNoteResponse>> GenerateObjectiveFindingsAsync(Guid sessionId, CancellationToken ct = default);
}
