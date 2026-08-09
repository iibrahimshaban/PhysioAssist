namespace PhysioAssist.Api.Shared.Dtos.Patient;

public class PatientPreferredTimeGroupDto
{
    public DaysOfWeekFlags Weekdays { get; set; }   // None for Today/Tomorrow/ThisWeek/etc.
    public TimeOnly? TimeFrom { get; set; }
    public TimeOnly? TimeTo { get; set; }
}
