namespace PhysioAssist.Api.Modules.Auth.Entities;

public class Clinic
{
    public Guid Id { get; set; }
    public string ClinicName { get; set; } = string.Empty;
    public string? ClinicAddress { get; set; }
}
