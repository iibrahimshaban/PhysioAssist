namespace PhysioAssist.Api.Modules.PatientModule.DTOs;

public class PatientWithNextSlotResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public PatientStatus Status { get; set; }
    public string QRCodeToken { get; set; } = string.Empty;
    public DateTime? SlotStart { get; set; }
    public DateTime? SlotEnd { get; set; }
}