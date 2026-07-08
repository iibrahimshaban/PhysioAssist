using PhysioAssist.Api.Modules.DocumentationModule.Errors;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Dtos.Documentation;

namespace PhysioAssist.Api.Modules.DocumentationModule.Services;

public class SessionProgressNoteService(ApplicationDbContext context) : ISessionProgressNoteService
{
    public async Task<Result<SessionProgressNoteResponse>> GetBySessionIdAsync(Guid sessionId, CancellationToken ct = default)
    {
        var note = await context.SessionProgressNotes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.SessionId == sessionId, ct);

        if (note is null)
            return Result.Failure<SessionProgressNoteResponse>(DocumentationErrors.ProgressNoteNotFound);

        return Result.Success(ToResponse(note));
    }

    public async Task<Result<SessionProgressNoteResponse>> UpdateNarrativeAsync(
        Guid sessionId, string subjective, string assessment, string plan, CancellationToken ct = default)
    {
        var note = await context.SessionProgressNotes
            .FirstOrDefaultAsync(n => n.SessionId == sessionId, ct);

        if (note is null)
            return Result.Failure<SessionProgressNoteResponse>(DocumentationErrors.ProgressNoteNotFound);

        note.Subjective = subjective;
        note.Assessment = assessment;
        note.Plan = plan;

        await context.SaveChangesAsync(ct);

        return Result.Success(ToResponse(note));
    }

    private static SessionProgressNoteResponse ToResponse(Entities.SessionProgressNote note) => new(
        note.Id, note.SessionId, note.DocumentationTemplateId,
        note.Subjective, note.ObjectiveFindings, note.Assessment, note.Plan);
}
