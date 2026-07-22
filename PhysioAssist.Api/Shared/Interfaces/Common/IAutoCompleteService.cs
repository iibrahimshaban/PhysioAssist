using PhysioAssist.Api.Infrastructure.AutoComplete.Models;

namespace PhysioAssist.Api.Shared.Interfaces.Common
{
    public interface IAutoCompleteService
    {
        /// <summary>
        /// List of words that matches the provided prefix 
        /// </summary>
        /// <param name="prefix">Word's prefix</param>
        /// <param name="limit">Number of words to return</param>
        /// <param name="ct">Cancelation token</param>
        /// <returns>List of read-only words</returns>
        Task<Result<IReadOnlyList<Suggestion>>> GetSuggestionsAsync(string prefix, int limit, CancellationToken ct);
    }
}
