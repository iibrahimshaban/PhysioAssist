namespace PhysioAssist.Api.Shared.Enums;

public class PatientTimePreferenceDto
{
    public RelativeDayToken DayToken { get; set; }
    public DaysOfWeekFlags PreferredWeekdays { get; set; } 
    public DateOnly? ExplicitDate { get; set; }
    public TimeOnly? PreferredTimeFrom { get; set; }
    public TimeOnly? PreferredTimeTo { get; set; }
}
