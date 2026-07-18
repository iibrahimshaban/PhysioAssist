namespace PhysioAssist.Api.Shared.Dtos.Doctor;

public class DoctorResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? ClinicAddress { get; set; }
    public string? About { get; set; }
    public int? YearsOfExperience { get; set; }
    public string UserId { get; set; } = string.Empty;
}
