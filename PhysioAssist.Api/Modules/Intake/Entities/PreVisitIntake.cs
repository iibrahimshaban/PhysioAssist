namespace PhysioAssist.Api.Modules.Intake.Entities;

public class PreVisitIntake
{
    public Guid Id { get; set; }
    public Guid DoctorId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string FormSubmissionData { get; set; } = string.Empty;
    public string PainPointsData { get; set; } = string.Empty;
    public IntakeStatus Status { get; set; } = IntakeStatus.Pending;
    public Guid? ConvertedToPatientId { get; set; }
    public DateTime SubmittedAt { get; set; }
}
