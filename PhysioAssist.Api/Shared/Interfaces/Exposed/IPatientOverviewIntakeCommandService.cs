namespace PhysioAssist.Api.Shared.Interfaces;

public interface IPatientOverviewIntakeCommandService
{
    Task<Result> UpdateOverviewDataAsync(
        Guid patientId,
        string formSubmissionData,
        string? painPointsData,
        CancellationToken ct = default);
}