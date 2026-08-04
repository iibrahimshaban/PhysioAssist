using PhysioAssist.Api.Modules.DocumentationModule.Contracts;

namespace PhysioAssist.Api.Modules.DocumentationModule.Services;

public class PatientDocumentationOverviewService(
    ApplicationDbContext context,
    ISessionQueryService sessionQueryService,
    IScheduleSlotQueryService scheduleSlotQueryService) : IPatientDocumentationOverviewService
{
    public async Task<List<PatientDocumentationSessionResponse>> GetSessionsAsync(
        Guid doctorId, Guid patientId, CancellationToken ct = default)
    {
        var sessions = await sessionQueryService.GetSessionsForDocumentationAsync(doctorId, patientId, ct);
        if (sessions.Count == 0)
            return [];

        var slotIds = sessions.Where(s => s.ScheduleSlotId.HasValue).Select(s => s.ScheduleSlotId!.Value);
        var slotsById = await scheduleSlotQueryService.GetSlotSummariesByIdsAsync(slotIds, ct);

        var sessionIds = sessions.Select(s => s.SessionId).ToList();
        var notesById = await context.SessionProgressNotes
            .AsNoTracking()
            .Where(n => sessionIds.Contains(n.SessionId))
            .Select(n => new { n.SessionId, LastEdited = n.UpdatedAt ?? n.CreatedAt })
            .ToDictionaryAsync(n => n.SessionId, n => n.LastEdited, ct);

        var results = new List<PatientDocumentationSessionResponse>();

        foreach (var s in sessions)
        {
            // Every session should have a slot — skip defensively rather than crash on data drift.
            if (!s.ScheduleSlotId.HasValue || !slotsById.TryGetValue(s.ScheduleSlotId.Value, out var slot))
                continue;

            var hasNote = notesById.TryGetValue(s.SessionId, out var noteLastEdited);
            var hasSummary = s.SummaryText is not null;
            var isStale = hasSummary && hasNote && s.SummaryGeneratedAt is not null && noteLastEdited > s.SummaryGeneratedAt;

            results.Add(new PatientDocumentationSessionResponse(
                s.SessionId,
                slot.SlotStart,
                (slot.SlotEnd - slot.SlotStart).TotalMinutes,
                slot.Status,
                hasNote,
                hasSummary,
                isStale,
                s.SummaryText));
        }

        return results.OrderByDescending(r => r.Date).ToList();
    }
}
