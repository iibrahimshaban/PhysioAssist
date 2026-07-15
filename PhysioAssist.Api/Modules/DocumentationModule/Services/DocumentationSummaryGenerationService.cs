using PhysioAssist.Api.Modules.DocumentationModule.Contracts;
using PhysioAssist.Api.Modules.DocumentationModule.Entities;
using PhysioAssist.Api.Modules.DocumentationModule.Errors;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Interfaces.Documentation;
using PhysioAssist.Api.Shared.Interfaces.Exposed;
using System.Text.Json;

namespace PhysioAssist.Api.Modules.DocumentationModule.Services;

public class DocumentationSummaryGenerationService(
    ApplicationDbContext context,
    ISessionQueryService sessionQueryService,
    IRollupSummarizationService rollupSummarizationService) : IDocumentationSummaryGenerationService
{
    public async Task<Result<DocumentationSummaryResponse>> GenerateAsync(
        Guid doctorId,
        Guid patientId,
        SummaryAudience audience,
        SummaryScope? scope,
        List<string>? focusAreas,
        CancellationToken ct = default)
    {
        var sessionSummaries = await sessionQueryService.GetSessionSummariesForPatientAsync(doctorId, patientId, ct);
        if (sessionSummaries.Count == 0)
            return Result.Failure<DocumentationSummaryResponse>(DocumentationErrors.NoSessionsFound);

        var sessionIds = sessionSummaries.Select(s => s.SessionId).ToList();

        var objectiveFindingsBySessionId = await context.SessionProgressNotes
            .AsNoTracking()
            .Where(n => sessionIds.Contains(n.SessionId))
            .Select(n => new { n.SessionId, n.ObjectiveFindings })
            .ToDictionaryAsync(n => n.SessionId, n => n.ObjectiveFindings, ct);

        var sessionInputs = sessionSummaries
            .Select(s => new SessionSummaryInput(
                s.NarrativeSummary,
                objectiveFindingsBySessionId.GetValueOrDefault(s.SessionId)))
            .ToList();

        var summaryText = await rollupSummarizationService.GenerateCaseSummaryAsync(
            sessionInputs, audience, scope, focusAreas, ct);

        if (summaryText is null)
            return Result.Failure<DocumentationSummaryResponse>(DocumentationErrors.RollupGenerationFailed);

        // Colleague exports strip identity by default; patient exports show full identity.
        var anonymize = audience == SummaryAudience.Colleague;

        var summary = new DocumentationSummary
        {
            Id = Guid.CreateVersion7(),
            DoctorId = doctorId,
            PatientId = patientId,
            Audience = audience,
            Scope = audience == SummaryAudience.Colleague ? scope : null,
            FocusAreas = focusAreas is { Count: > 0 } ? JsonSerializer.Serialize(focusAreas) : null,
            AnonymizePersonalData = anonymize,
            SummaryText = summaryText,
            FileUrl = string.Empty, // set by the PDF generation step, not yet run
            IsDeleted = false
        };

        // Append-only — every generation is its own audit record, never overwritten.
        context.DocumentationSummaries.Add(summary);
        await context.SaveChangesAsync(ct);

        return Result.Success(new DocumentationSummaryResponse(
            summary.Id, summary.Audience, summary.Scope, focusAreas,
            summary.AnonymizePersonalData, summary.SummaryText, summary.FileUrl, summary.CreatedAt));
    }
}
