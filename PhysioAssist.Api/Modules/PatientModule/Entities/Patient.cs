namespace PhysioAssist.Api.Modules.PatientModule.Entities;

public class Patient : AuditableEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ClinicId { get; set; }
    public Auth.Entities.Clinic Clinic { get; set; } = default!;
    public string FullName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string? PatientFreeTime { get; set; } = string.Empty;
    public RelativeDayToken ParsedPreferredDayToken { get; set; } = RelativeDayToken.Unspecified;
    public DateOnly? ParsedPreferredExplicitDate { get; set; }
    // REMOVE: ParsedPreferredTimeFrom, ParsedPreferredTimeTo, ParsedPreferredWeekdays
    public ICollection<PatientPreferredTimeSlot> PreferredTimeSlots { get; set; } = [];
    public string QRCodeToken { get; set; } = string.Empty;
    public string? PatientCaseNotes { get; set; } = string.Empty;
    public PatientStatus Status { get; set; } = PatientStatus.Active;
    public ICollection<DoctorPatient> DoctorPatients { get; set; } = [];
}
