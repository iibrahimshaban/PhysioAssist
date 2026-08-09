using PhysioAssist.Api.Shared.Dtos.Patient;

namespace PhysioAssist.Api.Shared.Interfaces.Scheduling;

public interface IPatientTimePreferenceParser
{
    Task<Result<PatientTimePreferenceDto>> ParseAsync(string englishFreeText, CancellationToken cancellationToken = default);
}
