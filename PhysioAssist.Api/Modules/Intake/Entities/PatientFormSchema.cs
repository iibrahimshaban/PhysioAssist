namespace PhysioAssist.Api.Modules.Intake.Entities;

public class PatientFormSchema : AuditableEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string SchemaJson { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public bool IsDefault { get; set; }
}
