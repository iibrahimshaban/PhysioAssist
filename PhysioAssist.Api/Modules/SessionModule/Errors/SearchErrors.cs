namespace PhysioAssist.Api.Modules.SessionModule.Errors;

public static class SearchErrors
{
    public static readonly Error EmptyQuery =
        new("Search.EmptyQuery", "Search query cannot be empty.", StatusCodes.Status400BadRequest);

    public static readonly Error TranslationFailed =
        new("Search.TranslationFailed", "Failed to translate the search query.", StatusCodes.Status500InternalServerError);

    public static readonly Error EmbeddingFailed =
        new("Search.EmbeddingFailed", "Failed to generate embedding for the search query.", StatusCodes.Status500InternalServerError);

    public static readonly Error SearchFailed =
        new("Search.SearchFailed", "Vector search failed.", StatusCodes.Status500InternalServerError);
}
