using PhysioAssist.Api.Shared.Dtos.Session;

namespace PhysioAssist.Api.Shared.Interfaces;

public interface ISessionQueryService
{
    Task<SessionTranscriptContext?> GetTranscriptContextAsync(Guid sessionId, CancellationToken ct = default);
    Task<List<SessionSummaryItem>> GetSessionSummariesForPatientAsync(
        Guid doctorId, Guid patientId, CancellationToken ct = default);
}
