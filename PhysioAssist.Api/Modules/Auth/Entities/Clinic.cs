using PhysioAssist.Api.Modules.PatientModule.Entities;

namespace PhysioAssist.Api.Modules.Auth.Entities;

public class Clinic
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ApplicationUser> Users { get; set; } = [];
    public ICollection<Patient> Patients { get; set; } = [];
}
