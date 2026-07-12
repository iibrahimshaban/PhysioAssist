namespace PhysioAssist.Api.Modules.Intake.DTOs.Submissions;

public record PreVisitIntakeResponse
{
    public Guid Id { get; init; }
    public Guid DoctorId { get; init; }
    public Guid FormSchemaId { get; init; }
    public int FormSchemaVersion { get; init; }
    public IntakeStatus Status { get; init; }
    public Guid? ConvertedToPatientId { get; init; }
    public DateTime SubmittedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public Guid? ReviewedByDoctorId { get; init; }
    public string? PatientName { get; init; }  
    public int PainRegionCount { get; init; }
}
