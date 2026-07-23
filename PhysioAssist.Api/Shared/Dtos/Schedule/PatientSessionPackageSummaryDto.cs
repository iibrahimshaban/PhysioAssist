namespace PhysioAssist.Api.Shared.Dtos.Schedule;

public class PatientSessionPackageSummaryDto
{
    public required Guid PackageId { get; init; }
    public required Guid PatientId { get; init; }
    public required Guid DoctorId { get; init; }
    public required int TotalSessions { get; init; }
    public required int ScheduledSessions { get; init; }
    public required int RemainingSessions { get; init; }
    public required int NextSessionNumber { get; init; }
    public required PackageStatus Status { get; init; }
    public required int SessionsPerWeek { get; init; }
    public required TimeSpan SessionDuration { get; init; }
    public required string PatientFreeTimeText { get; init; }
}
