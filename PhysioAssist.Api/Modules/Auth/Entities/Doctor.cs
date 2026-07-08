namespace PhysioAssist.Api.Modules.Auth.Entities;

public class Doctor
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string ClinicName { get; set; } = string.Empty;
    public string? Title { get; set; }              // e.g. "Senior Physiotherapist"
    public string? ClinicAddress { get; set; }
    public string? About { get; set; }
    public int? YearsOfExperience { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = default!;
}
