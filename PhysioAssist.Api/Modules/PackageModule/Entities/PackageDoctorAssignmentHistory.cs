namespace PhysioAssist.Api.Modules.PackageModule.Entities;

public class PackageDoctorAssignmentHistory
{
    public Guid UniqueId { get; set; }

    // FK -> PatientSessionPackage
    public Guid PackageId { get; set; }
    public PatientSessionPackage Package { get; set; } = default!;

    public Guid OldDoctorId { get; set; }

    public Guid NewDoctorId { get; set; }

    public PackageDoctorAssignmentChangeType ChangeType { get; set; }

    // Only populated when ChangeType == SingleSessionSubstitution
    public Guid? SessionId { get; set; }

    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }

    public Guid ChangedByUserId { get; set; }
}
