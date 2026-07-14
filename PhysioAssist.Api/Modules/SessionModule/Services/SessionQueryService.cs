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
}
