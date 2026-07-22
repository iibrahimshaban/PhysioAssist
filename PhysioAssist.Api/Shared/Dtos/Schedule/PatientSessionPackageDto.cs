namespace PhysioAssist.Api.Shared.Dtos.Schedule;

public class PatientSessionPackageDto
{
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public Guid DoctorId { get; init; }
    public int TotalSessions { get; init; }
    public int ScheduledSessions { get; init; }
    public int RemainingSessions { get; init; }
    public PackageStatus Status { get; init; }
    public Guid FirstScheduleSlotId { get; init; }
}
