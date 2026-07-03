using PhysioAssist.Api.Infrastructure.AutoComplete.Models;

namespace PhysioAssist.Api.Shared.Interfaces
{
    public interface IAutoCompleteService
    {
        IReadOnlyList<Suggestion> GetSuggestions(string prefix, int limit = 8);
    }
}
