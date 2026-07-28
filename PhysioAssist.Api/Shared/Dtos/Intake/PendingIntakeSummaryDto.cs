namespace PhysioAssist.Api.Shared.Dtos.Intake;

public record PendingIntakeSummaryDto(
    Guid Id,
    string PatientFullName,
    DateTimeOffset SubmittedAt,
    int PainRegionsCount);

public record PendingIntakesResult(int TotalCount, IReadOnlyList<PendingIntakeSummaryDto> Items);
