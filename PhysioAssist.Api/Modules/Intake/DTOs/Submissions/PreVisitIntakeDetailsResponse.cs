namespace PhysioAssist.Api.Modules.Intake.DTOs.Submissions;

public record PreVisitIntakeDetailsResponse
{
    public Guid Id { get; init; }
    public Guid DoctorId { get; init; }
    public Guid FormSchemaId { get; init; }
    public int FormSchemaVersion { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string? PatientEmail { get; init; }
    public string? PatientPhone { get; init; }
    public string FormSubmissionData { get; init; } = string.Empty;
    public string? PainPointsData { get; init; }
    public IntakeStatus Status { get; init; }
    public Guid? ConvertedToPatientId { get; init; }
    public DateTime SubmittedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public Guid? ReviewedByDoctorId { get; init; }
    public string FormSchemaName { get; init; } = string.Empty;
}
