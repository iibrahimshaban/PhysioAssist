namespace PhysioAssist.Api.Modules.Auth.Entities;

public class Receptionist
{
    public Guid Id { get; set; }
    public TimeOnly? From { get; set; }
    public TimeOnly? To { get; set; }

    public ReceptionistShiftType Shift { get; set; }

    public Guid ManagingDoctorId { get; set; }
    public Doctor ManagingDoctor { get; set; } = default!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = default!;
}
