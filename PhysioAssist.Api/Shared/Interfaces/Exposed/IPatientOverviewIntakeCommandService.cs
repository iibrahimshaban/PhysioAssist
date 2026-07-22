namespace PhysioAssist.Api.Shared.Interfaces;

public interface IPatientOverviewIntakeCommandService
{
    Task<Result> UpdateFormSubmissionDataAsync(Guid patientId, string formSubmissionData, CancellationToken ct = default);
}