using PhysioAssist.Api.Modules.SessionModule.Contracts;

namespace PhysioAssist.Api.Modules.SessionModule.Services
{
    public interface ISessionService
    {
         Task<Result<SessionResponse>> CreateSessionAsync(CreateSessionRequest request);
        Task<Result> StartSessionAsync(Guid id);
        Task<Result<SessionResponse>> GetSessionByIdAsync(Guid id);
        Task<Result<SessionDetailsResponse>> GetSessionDetailsAsync(Guid id);
        Task<Result<string>> CreateAudioTranscriptionAsync(
            Guid sessionId,
            CreateAudioTranscriptionRequest request,
            CancellationToken cancellationToken = default
            );
        Task<Result> UploadAttachmentsAsync(
            Guid sessionId,
            UploadSessionAttachmentRequest request,
            CancellationToken cancellationToken = default
            );
        Task<Result> CompleteSessionAsync(
    Guid sessionId,
    CompleteSessionRequest request,
    CancellationToken cancellationToken = default);
        Task<Result> SaveSessionDraftAsync(
    Guid sessionId,
    SaveSessionDraftRequest request,
    CancellationToken cancellationToken = default);
        Task<Result> DeleteAttachmentAsync(
    Guid attachmentId,
    CancellationToken cancellationToken = default);
    }
}
