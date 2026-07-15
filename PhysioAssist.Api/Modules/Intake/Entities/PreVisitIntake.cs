namespace PhysioAssist.Api.Modules.Intake.Entities;

public class PreVisitIntake
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid DoctorId { get; set; }
    public Guid FormSchemaId { get; set; }
    public int FormSchemaVersion { get; set; }
    public string FormSubmissionData { get; set; } = string.Empty;
    public string? PainPointsData { get; set; }
    public IntakeStatus Status { get; set; } = IntakeStatus.Pending;
    public Guid? ConvertedToPatientId { get; set; }
    public string? AccessTokenHash { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByDoctorId { get; set; }

    // Navigation property
    public PatientFormSchema? FormSchema { get; set; }
}
