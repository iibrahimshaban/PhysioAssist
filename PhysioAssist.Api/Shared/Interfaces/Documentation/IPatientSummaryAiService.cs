namespace PhysioAssist.Api.Shared.Interfaces.Documentation;

public interface IPatientSummaryAiService
{
    Task<Result<string>> GeneratePatientFriendlySummaryAsync(string clinicalReportText, CancellationToken ct = default);
}
