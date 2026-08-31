namespace PhysioAssist.Api.Shared.Dtos.Package;

public class SubstituteDoctorForSessionRequest
{
    public required Guid SessionId { get; init; }
    public required Guid NewDoctorId { get; init; }
    public string? Reason { get; init; }
}
