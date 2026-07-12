namespace PhysioAssist.Api.Modules.PatientModule.Entities;

public class DoctorPatient
{
    public Guid DoctorId { get; set; }
    public Guid PatientId { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime AssignedAt { get; set; }
    public AccessLevel AccessLevel { get; set; }
    public DoctorPatientStatus Status { get; set; } = DoctorPatientStatus.Active;
    public PatientCategory Category { get; set; } 
    public Patient Patient { get; set; } = default!;
}
