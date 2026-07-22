namespace PhysioAssist.Api.Shared.Interfaces.Scheduling;

public interface IPatientTimePreferenceParser
{
    Task<Result<PatientTimePreferenceDto>> ParseAsync(string englishFreeText, CancellationToken cancellationToken = default);
}
