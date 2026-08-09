namespace PhysioAssist.Api.Modules.PatientModule.Entities;

public class PatientPreferredTimeSlot
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public DaysOfWeekFlags Weekdays { get; set; }   // e.g. Saturday, or Monday|Wednesday
    public TimeOnly? TimeFrom { get; set; }
    public TimeOnly? TimeTo { get; set; }
}
