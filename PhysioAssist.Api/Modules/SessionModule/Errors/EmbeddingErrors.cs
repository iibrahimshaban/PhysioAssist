namespace PhysioAssist.Api.Modules.SessionModule.Errors;

public static class EmbeddingErrors
{
    public static readonly Error EmptyText =
        new("Embedding.EmptyText", "Transcript text is empty.", StatusCodes.Status400BadRequest);

    public static readonly Error TranscriptionNotFound =
        new("Embedding.TranscriptionNotFound", "The session transcription was not found.", StatusCodes.Status404NotFound);

    public static readonly Error ChunkingFailed =
        new("Embedding.ChunkingFailed", "Failed to chunk transcript text.", StatusCodes.Status500InternalServerError);

    public static readonly Error NoChunks =
        new("Embedding.NoChunks", "No chunks were produced from the input text.", StatusCodes.Status500InternalServerError);

    public static readonly Error GenerationFailed =
        new("Embedding.GenerationFailed", "Failed to generate embedding for a transcript chunk.", StatusCodes.Status500InternalServerError);

    public static readonly Error SaveFailed =
        new("Embedding.SaveFailed", "Failed to save transcript chunks.", StatusCodes.Status500InternalServerError);
}
