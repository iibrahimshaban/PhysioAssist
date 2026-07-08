namespace PhysioAssist.Api.Shared.Dtos.Session;

public sealed record SessionTranscriptContext(
    Guid SessionId,
    Guid DoctorId,
    Guid PatientId,
    string TranscriptText);
