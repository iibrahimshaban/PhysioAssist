using PhysioAssist.Api.Infrastructure.AutoComplete.Models;

namespace PhysioAssist.Api.Shared.Interfaces
{
    public interface IAutoCompleteService
    {
        Task<IReadOnlyList<Suggestion>> GetSuggestionsAsync(string prefix, int limit, CancellationToken ct);
    }
}
