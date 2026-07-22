namespace PhysioAssist.Api.Modules.PatientModule.DTOs;

public class PatientOverviewResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public PatientStatus Status { get; set; }

    public string? FormSubmissionData { get; set; }   // raw JSON — patient answers
    public string? PainPointsJson { get; set; }         // raw JSON — { regions }
    public string? DoctorInfoJson { get; set; }         // raw JSON — { chiefComplaint, patientCategory }
}