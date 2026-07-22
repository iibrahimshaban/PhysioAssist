namespace PhysioAssist.Api.Shared.Dtos.Patient;

public class PatientResponse
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string FullName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string? Occupation { get; set; }
    public string QRCodeToken { get; set; } = string.Empty;
    public PatientStatus Status { get; set; } = PatientStatus.Active;
}
