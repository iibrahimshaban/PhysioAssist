namespace PhysioAssist.Api.Modules.PatientModule.Entities;

public class PatientArchive
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string FileUrl { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
}
