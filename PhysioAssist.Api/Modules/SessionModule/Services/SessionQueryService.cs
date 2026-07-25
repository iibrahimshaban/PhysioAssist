using PhysioAssist.Api.Modules.SessionModule.Entities;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Dtos.Session;
using PhysioAssist.Api.Shared.Interfaces.Exposed;

namespace PhysioAssist.Api.Modules.SessionModule.Services;

public class SessionQueryService(ApplicationDbContext context) : ISessionQueryService
{
    public async Task<SessionTranscriptContext?> GetTranscriptContextAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await context.Sessions
            .AsNoTracking()
            .Include(s => s.Transcription)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session?.Transcription is null)
            return null;

        // Prefer the doctor-reviewed/edited transcript over the raw ASR output when available.
        var transcriptText = !string.IsNullOrWhiteSpace(session.Transcription.EditedTranscript)
            ? session.Transcription.EditedTranscript
            : session.Transcription.RawTranscript;

        if (string.IsNullOrWhiteSpace(transcriptText))
            return null;

        return new SessionTranscriptContext(session.Id, session.DoctorId, session.PatientId, transcriptText);
    }
    public async Task<List<SessionSummaryItem>> GetSessionSummariesForPatientAsync(
        Guid doctorId, Guid patientId, CancellationToken ct = default)
    {
        return await context.Sessions
            .AsNoTracking()
            .Where(s => s.DoctorId == doctorId && s.PatientId == patientId && s.SummaryText != null)
            .Select(s => new SessionSummaryItem(s.Id, s.SummaryText, s.SummaryGeneratedAt))
            .ToListAsync(ct);
    }
    public async Task<Dictionary<Guid, SessionLookupDto>> GetSessionsByScheduleSlotIdsAsync(
        IEnumerable<Guid> scheduleSlotIds,
        CancellationToken cancellationToken = default)
    {
        var ids = scheduleSlotIds.Distinct().ToList();

        return await context.Set<Session>()
            .Where(s => s.ScheduleSlotId.HasValue && ids.Contains(s.ScheduleSlotId.Value))
            .Select(s => new SessionLookupDto(s.Id, s.ScheduleSlotId!.Value, s.Status))
            .ToDictionaryAsync(s => s.ScheduleSlotId, cancellationToken);
    }

    public async Task<Guid?> GetScheduleSlotIdBySessionIdAsync(Guid sessionId,CancellationToken cancellationToken = default)
    {
        return await context.Set<Session>()
            .Where(s => s.Id == sessionId)
            .Select(s => s.ScheduleSlotId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
