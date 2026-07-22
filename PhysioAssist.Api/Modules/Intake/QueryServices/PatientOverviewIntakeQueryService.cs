using PhysioAssist.Api.Modules.Intake.Errors;
using PhysioAssist.Api.Modules.Intake.Helpers;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Dtos.Intake;
using PhysioAssist.Api.Shared.Interfaces;

namespace PhysioAssist.Api.Modules.Intake.QueryServices;

public class PatientOverviewIntakeQueryService(ApplicationDbContext context) : IPatientOverviewIntakeQueryService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<Result<PatientOverviewIntakeResult>> GetOverviewDataForPatientAsync(Guid patientId, CancellationToken ct = default)
    {
        var intake = await _context.PreVisitIntakes
            .AsNoTracking()
            .Where(x => x.ConvertedToPatientId == patientId)
            .OrderByDescending(x => x.SubmittedAt)
            .FirstOrDefaultAsync(ct);

        if (intake is null)
            return Result.Failure<PatientOverviewIntakeResult>(IntakeErrors.SubmissionNotFound);

        var painPointsJson = PainPointsSplitHelper.ExtractPainPointsJson(intake.PainPointsData);
        var doctorInfoJson = PainPointsSplitHelper.ExtractDoctorInfoJson(intake.PainPointsData);

        var result = new PatientOverviewIntakeResult(
            intake.FormSubmissionData,   // raw, untouched
            painPointsJson,
            doctorInfoJson
        );

        return Result.Success(result);
    }
}