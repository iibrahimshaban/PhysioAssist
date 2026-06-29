
namespace PhysioAssist.Api.Modules.PatientModule.Entities;

public class Patient : AuditableEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string QRCodeToken { get; set; } = string.Empty;
    public PatientStatus Status { get; set; } = PatientStatus.Active;
    public ICollection<DoctorPatient> DoctorPatients { get; set; } = [];
}
