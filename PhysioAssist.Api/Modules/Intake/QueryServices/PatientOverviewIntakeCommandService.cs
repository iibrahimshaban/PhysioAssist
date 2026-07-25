using PhysioAssist.Api.Modules.Intake.Errors;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Interfaces;

namespace PhysioAssist.Api.Modules.Intake.QueryServices;

public class PatientOverviewIntakeCommandService(ApplicationDbContext context) : IPatientOverviewIntakeCommandService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<Result> UpdateFormSubmissionDataAsync(Guid patientId, string formSubmissionData, CancellationToken ct = default)
    {
        var intake = await _context.PreVisitIntakes
            .Where(x => x.ConvertedToPatientId == patientId)
            .OrderByDescending(x => x.SubmittedAt)
            .FirstOrDefaultAsync(ct);

        if (intake is null)
            return Result.Failure(IntakeErrors.SubmissionNotFound);

        intake.FormSubmissionData = formSubmissionData;

        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}