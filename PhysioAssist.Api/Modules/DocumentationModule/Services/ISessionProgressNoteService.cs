using PhysioAssist.Api.Shared.Dtos.Documentation;

namespace PhysioAssist.Api.Modules.DocumentationModule.Services;

public interface ISessionProgressNoteService
{
    Task<Result<SessionProgressNoteResponse>> GetBySessionIdAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Updates the doctor-authored Subjective/Assessment/Plan text. Does not touch
    /// ObjectiveFindings — that's only ever written by ISessionProgressNoteExtractionService.
    /// Fails if no note exists yet for this session (generate objective findings first).
    /// </summary>
    Task<Result<SessionProgressNoteResponse>> UpdateNarrativeAsync(
        Guid sessionId, string subjective, string assessment, string plan, CancellationToken ct = default);
}
