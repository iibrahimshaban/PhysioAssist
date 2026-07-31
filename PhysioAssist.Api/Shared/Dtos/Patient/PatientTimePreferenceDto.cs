namespace PhysioAssist.Api.Shared.Dtos.Patient;

public class PatientTimePreferenceDto
{
    public RelativeDayToken DayToken { get; set; }
    public DateOnly? ExplicitDate { get; set; }
    public List<PatientPreferredTimeGroupDto> Groups { get; set; } = [];
}
