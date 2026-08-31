namespace PhysioAssist.Api.Shared.Dtos.Package;

public class ReassignPackageDoctorRequest
{
    public required Guid NewDoctorId { get; init; }
    public string? Reason { get; init; }
}
