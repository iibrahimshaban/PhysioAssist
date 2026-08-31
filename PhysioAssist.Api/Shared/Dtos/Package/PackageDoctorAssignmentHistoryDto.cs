namespace PhysioAssist.Api.Shared.Dtos.Package;

public class PackageDoctorAssignmentHistoryDto
{
    public required Guid UniqueId { get; init; }
    public required Guid PackageId { get; init; }
    public required Guid OldDoctorId { get; init; }
    public required Guid NewDoctorId { get; init; }
    public required string ChangeType { get; init; }
    public Guid? SessionId { get; init; }
    public required DateTime ChangedAt { get; init; }
    public string? Reason { get; init; }
    public required Guid ChangedByUserId { get; init; }
}
