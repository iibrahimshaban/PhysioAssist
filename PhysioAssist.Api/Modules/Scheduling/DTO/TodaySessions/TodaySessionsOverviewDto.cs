namespace PhysioAssist.Api.Modules.Scheduling.DTO.TodaySessions;

public sealed class TodaySessionsOverviewDto
{
    public DateOnly Date { get; init; }
    public int TotalToday { get; init; }
    public int CompletedCount { get; init; }
    public int InProgressCount { get; init; }
    public int UpNextCount { get; init; }
    public int MissedCount { get; init; }
    public double PercentDone { get; init; }
    public IReadOnlyList<TimelineMarkerDto> Timeline { get; init; } = [];
    public IReadOnlyList<TodaySessionCardDto> InProgress { get; init; } = [];
    public IReadOnlyList<TodaySessionCardDto> UpNext { get; init; } = [];
    public IReadOnlyList<TodaySessionCardDto> Completed { get; init; } = [];
    public IReadOnlyList<TodaySessionCardDto> Missed { get; init; } = [];
}
