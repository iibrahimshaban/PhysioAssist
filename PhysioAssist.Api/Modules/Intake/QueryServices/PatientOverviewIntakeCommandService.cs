using PhysioAssist.Api.Modules.Intake.Errors;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Interfaces;

namespace PhysioAssist.Api.Modules.Intake.QueryServices;

public class PatientOverviewIntakeCommandService(ApplicationDbContext context) : IPatientOverviewIntakeCommandService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<Result> UpdateOverviewDataAsync(
        Guid patientId,
        string formSubmissionData,
        string? painPointsData,
        CancellationToken ct = default)
    {
        var intake = await _context.PreVisitIntakes
            .Where(x => x.ConvertedToPatientId == patientId)
            .OrderByDescending(x => x.SubmittedAt)
            .FirstOrDefaultAsync(ct);

        if (intake is null)
            return Result.Failure(IntakeErrors.SubmissionNotFound);

        intake.FormSubmissionData = formSubmissionData;

        // null means "leave pain points untouched" (e.g. a pure form-answer edit
        // where the doctor didn't open/change the pain map at all)
        if (painPointsData is not null)
            intake.PainPointsData = painPointsData;

        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}