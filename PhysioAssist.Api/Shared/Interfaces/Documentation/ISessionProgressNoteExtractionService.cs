using PhysioAssist.Api.Shared.Dtos.Documentation;

namespace PhysioAssist.Api.Shared.Interfaces.Documentation;

public interface ISessionProgressNoteExtractionService
{
    Task<Result<SessionProgressNoteResponse>> GenerateObjectiveFindingsAsync(Guid sessionId, CancellationToken ct = default);
}
