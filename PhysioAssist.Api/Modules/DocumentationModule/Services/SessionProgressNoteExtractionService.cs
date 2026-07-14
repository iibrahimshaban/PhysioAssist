using PhysioAssist.Api.Modules.DocumentationModule.Entities;
using PhysioAssist.Api.Modules.DocumentationModule.Errors;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Dtos.Documentation;

namespace PhysioAssist.Api.Modules.DocumentationModule.Services;

public class SessionProgressNoteExtractionService(
    ApplicationDbContext context,
    ISessionQueryService sessionQueryService,
    IPatientQueryService patientQueryService,
    IDocumentationTemplateResolver templateResolver,
    IDocumentationExtractionService extractionService) : ISessionProgressNoteExtractionService
{
    public async Task<Result<SessionProgressNoteResponse>> GenerateObjectiveFindingsAsync(Guid sessionId, CancellationToken ct = default)
    {
        var transcriptContext = await sessionQueryService.GetTranscriptContextAsync(sessionId, ct);

        if (transcriptContext is null)
            return Result.Failure<SessionProgressNoteResponse>(DocumentationErrors.TranscriptNotFound);

        var category = await patientQueryService.GetPatientCategoryAsync(
            transcriptContext.DoctorId, transcriptContext.PatientId, ct);

        if (category is null)
            return Result.Failure<SessionProgressNoteResponse>(DocumentationErrors.CategoryNotSet);

        var template = await context.DocumentationTemplates
            .AsNoTracking()
            .Where(t => t.Category == category && t.IsActive)
            .FirstOrDefaultAsync(ct);

        if (template is null)
            return Result.Failure<SessionProgressNoteResponse>(DocumentationErrors.TemplateNotFound);

        var effectiveFieldsResult = await templateResolver.GetEffectiveFieldsAsync(transcriptContext.DoctorId, template.Id);
        if (effectiveFieldsResult.IsFailure)
            return Result.Failure<SessionProgressNoteResponse>(effectiveFieldsResult.Error);

        var objectiveFindings = await extractionService.ExtractObjectiveFindingsAsync(
            transcriptContext.TranscriptText, effectiveFieldsResult.Value, ct);

        if (objectiveFindings is null)
            return Result.Failure<SessionProgressNoteResponse>(DocumentationErrors.ExtractionFailed);

        // One SessionProgressNote per session — upsert rather than duplicate on regeneration.
        var note = await context.SessionProgressNotes
            .FirstOrDefaultAsync(n => n.SessionId == sessionId, ct);

        if (note is null)
        {
            note = new SessionProgressNote
            {
                Id = Guid.CreateVersion7(),
                SessionId = sessionId,
                DocumentationTemplateId = template.Id,
                Subjective = string.Empty,
                Assessment = string.Empty,
                Plan = string.Empty,
                ObjectiveFindings = objectiveFindings
            };
            context.SessionProgressNotes.Add(note);
        }
        else
        {
            note.ObjectiveFindings = objectiveFindings;
        }

        await context.SaveChangesAsync(ct);

        return Result.Success(new SessionProgressNoteResponse(
            note.Id, note.SessionId, note.DocumentationTemplateId,
            note.Subjective, note.ObjectiveFindings, note.Assessment, note.Plan));
    }
}
