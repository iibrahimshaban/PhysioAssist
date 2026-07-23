namespace PhysioAssist.Api.Modules.PatientModule.Entities;

public class Patient : AuditableEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string FullName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string? Occupation { get; set; }
    public string? PatientFreeTime { get; set; } = string.Empty;
    public RelativeDayToken ParsedPreferredDayToken { get; set; } = RelativeDayToken.Unspecified;
    public TimeOnly? ParsedPreferredTimeFrom { get; set; }
    public TimeOnly? ParsedPreferredTimeTo { get; set; }
    public DaysOfWeekFlags ParsedPreferredWeekdays { get; set; } // stored as int bitmask
    public string QRCodeToken { get; set; } = string.Empty;
    public PatientStatus Status { get; set; } = PatientStatus.Active;
    public ICollection<DoctorPatient> DoctorPatients { get; set; } = [];
}
