namespace PhysioAssist.Api.Infrastructure.AutoComplete.Models
{
    //public record Suggestion(string Term, string? Category, int Score);
    public record Suggestion(string Term, string? Category, double Score, string? Language);
}
