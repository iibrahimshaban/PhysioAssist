using PhysioAssist.Api.Modules.DocumentationModule.Errors;
using PhysioAssist.Api.Persistence;

namespace PhysioAssist.Api.Modules.DocumentationModule.Services;

public class SessionSummaryGenerationService(
    ApplicationDbContext context,
    ISessionSummarizationService summarizationService,
    ISessionSummaryWriter summaryWriter) : ISessionSummaryGenerationService
{
    public async Task<Result> GenerateAndSaveSummaryAsync(Guid sessionId, CancellationToken ct = default)
    {
        var note = await context.SessionProgressNotes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.SessionId == sessionId, ct);

        if (note is null)
            return Result.Failure(DocumentationErrors.ProgressNoteNotFound);

        var summaryText = await summarizationService.SummarizeSessionAsync(
            note.Subjective, note.ObjectiveFindings, note.Assessment, note.Plan, ct);

        if (summaryText is null)
            return Result.Failure(DocumentationErrors.SummaryGenerationFailed);

        var saved = await summaryWriter.SaveSummaryAsync(sessionId, summaryText, ct);
        if (!saved)
            return Result.Failure(DocumentationErrors.SessionNotFound);

        return Result.Success();
    }
}
