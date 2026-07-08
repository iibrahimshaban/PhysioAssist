namespace PhysioAssist.Api.Shared.Dtos.Chunking;

public sealed record ChunkSearchResult(
    int Id,
    Guid SessionTranscriptionId,
    Guid SessionId,
    Guid PatientId,
    Guid DoctorId,
    string Recommendations,
    string RecommendationDetails,
    string? PatientResponse,
    string NextSessionFocus,
    string Diagnosis,
    string? Notes,
    double Distance
    );
