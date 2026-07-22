using PhysioAssist.Api.Modules.Intake.Errors;
using PhysioAssist.Api.Modules.Intake.Helpers;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Dtos.Intake;
using PhysioAssist.Api.Shared.Interfaces.Exposed;

namespace PhysioAssist.Api.Modules.Intake.Services;

public class IntakeQueryService(ApplicationDbContext context) : IIntakeQueryService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<Result<PreVisitIntakeDataResponse>> GetPreVisitIntakeByPatientIdAsync(Guid patientId)
    {
        var intake = await _context.PreVisitIntakes
            .AsNoTracking()
            .Where(x => x.ConvertedToPatientId == patientId)
            .OrderByDescending(x => x.SubmittedAt)
            .FirstOrDefaultAsync();

        if (intake is null)
        {
            return Result.Failure<PreVisitIntakeDataResponse>(IntakeErrors.SubmissionNotFound);
        }

        var response = new PreVisitIntakeDataResponse(
            intake.Id,
            intake.DoctorId,
            intake.FormSchemaId,  
            intake.FormSchemaVersion,
            intake.FormSubmissionData, 
            intake.PainPointsData,
            intake.Status,
            intake.ConvertedToPatientId,
            intake.SubmittedAt,
            intake.ReviewedAt,
            intake.ReviewedByDoctorId);

        return Result.Success(response);
    }

    public async Task<Result<PatientIntakeSummaryResponse>> GetPatientIntakeSummaryAsync(Guid patientId)
    {
        var intake = await _context.PreVisitIntakes
            .AsNoTracking()
            .Where(x => x.ConvertedToPatientId == patientId)
            .OrderByDescending(x => x.SubmittedAt)
            .FirstOrDefaultAsync();

        if (intake is null)
        {
            return Result.Failure<PatientIntakeSummaryResponse>(IntakeErrors.SubmissionNotFound);
        }

        var submission = ExtractInputValuesHelper.DeserializeSubmissionJson(intake.FormSubmissionData);

        string? fullName = null;
        string? gender = null;
        int? age = null;
        DateTime? injuryDate = null;

        if (submission is not null)
        {
            fullName = ExtractInputValuesHelper.ExtractAnswerString(submission, "question_default_full_name", "text");
            gender = ExtractInputValuesHelper.ExtractAnswerString(submission, "question_default_gender", "radio");
            var dob = ExtractInputValuesHelper.ExtractAnswerDate(submission, "question_default_dob", "date");
            age = dob.HasValue ? ExtractInputValuesHelper.CalculateAge(dob.Value) : null;
            injuryDate = ExtractInputValuesHelper.ExtractAnswerDate(submission, "question_default_injury_date", "date");
        }

        var chiefComplaint = ExtractInputValuesHelper.ExtractChiefComplaint(intake.PainPointsData);
        var injury = ExtractInputValuesHelper.ExtractInjury(intake.PainPointsData);
        var patientCategory = ExtractInputValuesHelper.ExtractPatientCategory(intake.PainPointsData);

        return Result.Success(new PatientIntakeSummaryResponse(fullName, gender, age, chiefComplaint, injury, injuryDate, patientCategory));
    }
}