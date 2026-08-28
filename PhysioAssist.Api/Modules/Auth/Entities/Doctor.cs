namespace PhysioAssist.Api.Modules.Auth.Entities;

public class Doctor
{
    public Guid Id { get; set; }   
    public string? Title { get; set; }            
    public string? About { get; set; }
    public int? YearsOfExperience { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = default!;
}
