using PhysioAssist.Api.Modules.Intake.Errors;
using PhysioAssist.Api.Modules.Intake.Helpers;
using PhysioAssist.Api.Shared.Dtos.Intake;

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
            return Result.Failure<PreVisitIntakeDataResponse>(IntakeErrors.SubmissionNotFound);

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
            return Result.Failure<PatientIntakeSummaryResponse>(IntakeErrors.SubmissionNotFound);

        var submission = ExtractInputValuesHelper.DeserializeSubmissionJson(intake.FormSubmissionData);

        string? fullName = null;
        string? gender = null;
        int? age = null;
        DateTime? injuryDate = null;
        string? chiefComplaint = null;
        string? patientType = null;
        var patientCategory = PatientCategory.GeneralOther;

        if (submission is not null)
        {
            fullName = ExtractInputValuesHelper.ExtractAnswerString(submission, IntakeQuestionIds.FullName, "text");
            gender = ExtractInputValuesHelper.ExtractAnswerString(submission, IntakeQuestionIds.Gender, "radio");

            var dob = ExtractInputValuesHelper.ExtractAnswerDate(submission, IntakeQuestionIds.DateOfBirth, "date");
            age = dob.HasValue ? ExtractInputValuesHelper.CalculateAge(dob.Value) : null;

            injuryDate = ExtractInputValuesHelper.ExtractAnswerDate(submission, IntakeQuestionIds.InjuryDate, "date");
            chiefComplaint = ExtractInputValuesHelper.ExtractAnswerString(submission, IntakeQuestionIds.ChiefComplaint, "textarea");
            patientType = ExtractInputValuesHelper.ExtractAnswerString(submission, IntakeQuestionIds.PatientType, "select");
            patientCategory = ExtractInputValuesHelper.ExtractPatientCategory(submission);
        }

        // Legacy fallback: older submissions predating the form fields stored chief
        // complaint/injury only in the pain map.
        chiefComplaint = string.IsNullOrWhiteSpace(chiefComplaint)
            ? ExtractInputValuesHelper.ExtractChiefComplaint(intake.PainPointsData)
            : chiefComplaint;

        var injury = ExtractInputValuesHelper.ExtractInjury(intake.PainPointsData);
        patientType = string.IsNullOrWhiteSpace(patientType) ? null : patientType;

        // Provide sensible defaults so no field shows as empty in the report summary.
        fullName = string.IsNullOrWhiteSpace(fullName) ? "Patient" : fullName;
        gender = string.IsNullOrWhiteSpace(gender) ? "Not Specified" : gender;
        chiefComplaint = string.IsNullOrWhiteSpace(chiefComplaint) ? "Pre-visit intake assessment & general physiotherapy evaluation." : chiefComplaint;
        injury = string.IsNullOrWhiteSpace(injury) ? "Pre-visit intake pain assessment." : injury;

        return Result.Success(new PatientIntakeSummaryResponse(fullName, gender, age, chiefComplaint, injury, injuryDate, patientCategory, patientType));
    }

    public async Task<Result<string?>> GetPatientFreeTimeTextAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var intake = await _context.PreVisitIntakes
            .Where(i => i.ConvertedToPatientId == patientId)
            .OrderByDescending(i => i.ReviewedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (intake is null)
            return Result.Success<string?>(null);

        var submission = ExtractInputValuesHelper.DeserializeSubmissionJson(intake.FormSubmissionData);
        if (submission is null)
            return Result.Success<string?>(null);

        var freeTimeText = ExtractInputValuesHelper.ExtractAnswerString(submission, IntakeQuestionIds.FreeTime, "text");
        return Result.Success(freeTimeText);
    }

    public async Task<Result<PendingIntakesResult>> GetPendingIntakesAsync(Guid doctorId, int take, CancellationToken cancellationToken = default)
    {
        var pendingQuery = _context.PreVisitIntakes
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId && x.Status == IntakeStatus.Pending);

        var totalCount = await pendingQuery.CountAsync(cancellationToken);

        var intakes = await pendingQuery
            .OrderBy(x => x.SubmittedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        var items = intakes
            .Select(intake => new PendingIntakeSummaryDto(
                intake.Id,
                ExtractInputValuesHelper.ExtractPatientNameSafe(intake.FormSubmissionData) ?? "Unknown Patient",
                intake.SubmittedAt,
                ExtractInputValuesHelper.CountPainRegions(intake.PainPointsData)))
            .ToList();

        return Result.Success(new PendingIntakesResult(totalCount, items));
    }
}