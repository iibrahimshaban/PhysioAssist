using PhysioAssist.Api.Shared.Dtos.Transcription;
using PhysioAssist.Api.Shared.Interfaces.Common;
using PhysioAssist.Api.Shared.Interfaces.Ingestion;

namespace PhysioAssist.Api.Infrastructure.GroqClient;

public class RefinedTranscriptionService(
    IAudioTranscriptionService _inner,
    ITranscriptionRefinementService _refinement) : IAudioTranscriptionService
{
    public async Task<Result<TranscriptionResult>> TranscribeAsync(
        TranscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var transcription = await _inner.TranscribeAsync(request, cancellationToken);
        if (transcription.IsFailure)
            return transcription;


        var refinement = await _refinement.RefineAsync(transcription.Value.RawText, cancellationToken);
        if (refinement.IsFailure)
            return transcription;         


        return Result.Success(transcription.Value with { RefinedText = refinement.Value });
    }
}
