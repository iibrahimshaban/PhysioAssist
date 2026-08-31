using PhysioAssist.Api.Shared.Dtos.Patient;

namespace PhysioAssist.Api.Shared.Dtos.Package;

public class PatientPackageHistoryItemDto
{
    public required Guid PackageId { get; init; }
    public required Guid TreatmentSchedulePlanId { get; init; }
    public required Guid ReportId { get; init; }
    public required PackageStatus Status { get; init; }
    public required int TotalSessions { get; init; }
    public required int CompletedSessions { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required IReadOnlyList<PatientSessionListItemDto> Sessions { get; init; }
}
