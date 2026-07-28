using PhysioAssist.Api.Modules.DashboardModule.Contracts;

namespace PhysioAssist.Api.Modules.DashboardModule.Services;

public class DoctorDashboardService(
    ITodaySessionsService todaySessionsService,
    IIntakeQueryService intakeQueryService) : IDoctorDashboardService
{
    private const int PendingIntakesPreviewLimit = 3;

    public async Task<Result<DoctorDashboardSummaryDto>> GetSummaryAsync(
        Guid doctorId,
        string doctorFirstName,
        CancellationToken cancellationToken = default)
    {
        var todaySessionsResult = await todaySessionsService.GetTodaySessionsAsync(doctorId, cancellationToken);
        if (todaySessionsResult.IsFailure)
            return Result.Failure<DoctorDashboardSummaryDto>(todaySessionsResult.Error);

        var today = todaySessionsResult.Value;
        var upcomingCount = today.UpNextCount + today.InProgressCount;

        var pendingResult = await intakeQueryService.GetPendingIntakesAsync(
            doctorId, PendingIntakesPreviewLimit, cancellationToken);

        if (pendingResult.IsFailure)
            return Result.Failure<DoctorDashboardSummaryDto>(pendingResult.Error);

        return Result.Success(new DoctorDashboardSummaryDto
        {
            DoctorFirstName = doctorFirstName,
            PendingIntakesCount = pendingResult.Value.TotalCount,
            UpcomingSessionsTodayCount = upcomingCount,
            PendingIntakes = pendingResult.Value.Items
                .Select(item => new PendingIntakePreviewDto
                {
                    SubmissionId = item.Id,
                    PatientFullName = item.PatientFullName,
                    SubmittedAt = item.SubmittedAt,
                    PainRegionsCount = item.PainRegionsCount,
                })
                .ToList(),
        });
    }
}
