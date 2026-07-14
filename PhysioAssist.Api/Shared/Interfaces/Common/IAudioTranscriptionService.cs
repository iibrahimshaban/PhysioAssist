using PhysioAssist.Api.Shared.Dtos.Transcription;

namespace PhysioAssist.Api.Shared.Interfaces.Common;

public interface IAudioTranscriptionService
{
    Task<Result<TranscriptionResult>> TranscribeAsync(TranscriptionRequest request, CancellationToken cancellationToken = default);
}
