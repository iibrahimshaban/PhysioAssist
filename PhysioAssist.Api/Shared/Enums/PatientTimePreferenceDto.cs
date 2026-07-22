namespace PhysioAssist.Api.Shared.Enums;

public class PatientTimePreferenceDto
{
    public RelativeDayToken DayToken { get; init; } = RelativeDayToken.Unspecified;

    /// <summary>Set only when the patient gave an unambiguous calendar date the model could resolve.</summary>
    public DateOnly? ExplicitDate { get; init; }

    public TimeOnly? PreferredTimeFrom { get; init; }
    public TimeOnly? PreferredTimeTo { get; init; }
}
