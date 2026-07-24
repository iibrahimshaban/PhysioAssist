namespace PhysioAssist.Api.Shared.Dtos.Patient;

public sealed class PatientScheduleOverviewDto
{
    public bool HasPackage { get; init; }
    public Guid? PackageId { get; init; }
    public PackageStatus? PackageStatus { get; init; }
    public int TotalSessions { get; init; }
    public int CompletedSessions { get; init; }
    public int RemainingSessions { get; init; }
    public int UpcomingScheduledCount { get; init; }
    public IReadOnlyList<PatientSessionListItemDto> Sessions { get; init; } = [];

    public static PatientScheduleOverviewDto Empty() => new() { HasPackage = false };
}
