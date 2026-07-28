namespace PhysioAssist.Api.Modules.DashboardModule.Contracts;

public record DoctorDashboardSummaryDto
{
    public string DoctorFirstName { get; init; } = string.Empty;
    public int PendingIntakesCount { get; init; }
    public int UpcomingSessionsTodayCount { get; init; }
    public IReadOnlyList<PendingIntakePreviewDto> PendingIntakes { get; init; } = [];
}
