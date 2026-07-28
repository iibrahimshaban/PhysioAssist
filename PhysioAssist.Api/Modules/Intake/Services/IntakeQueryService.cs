using PhysioAssist.Api.Modules.Intake.Entities;
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

        // Load the schema once (used for dynamic text-based lookups) so both the
        // personal fields and the Chief Complaint form-field read can use it.
        var schema = submission is not null
            ? await LoadFormSchemaAsync(intake.FormSchemaId)
            : null;

        string? fullName = null;
        string? gender = null;
        int? age = null;
        DateTime? injuryDate = null;

        if (submission is not null)
        {
            if (schema is not null)
            {
                // Dynamic lookup by text (handles customized forms)
                var fullNameQId = ExtractInputValuesHelper.FindQuestionIdByText(schema, "Full Name")
                    ?? ExtractInputValuesHelper.FindQuestionIdByText(schema, "Name");
                var genderQId = ExtractInputValuesHelper.FindQuestionIdByText(schema, "Gender");
                var dobQId = ExtractInputValuesHelper.FindQuestionIdByText(schema, "Date of Birth")
                    ?? ExtractInputValuesHelper.FindQuestionIdByText(schema, "DOB");
                var injuryDateQId = ExtractInputValuesHelper.FindQuestionIdByText(schema, "Injury Date");

                if (fullNameQId is not null)
                    fullName = ExtractInputValuesHelper.ExtractAnswerString(submission, fullNameQId, ExtractInputValuesHelper.GetWrapperKey(schema, fullNameQId));
                if (genderQId is not null)
                    gender = ExtractInputValuesHelper.ExtractAnswerString(submission, genderQId, ExtractInputValuesHelper.GetWrapperKey(schema, genderQId));
                if (dobQId is not null)
                {
                    var dob = ExtractInputValuesHelper.ExtractAnswerDate(submission, dobQId, ExtractInputValuesHelper.GetWrapperKey(schema, dobQId));
                    age = dob.HasValue ? ExtractInputValuesHelper.CalculateAge(dob.Value) : null;
                }
                if (injuryDateQId is not null)
                    injuryDate = ExtractInputValuesHelper.ExtractAnswerDate(submission, injuryDateQId, ExtractInputValuesHelper.GetWrapperKey(schema, injuryDateQId));
            }
            else
            {
                // Fallback to hardcoded IDs if schema can't be loaded (backward compatibility).
                // Prefer the canonical question_default_* ids (per requirements), then core_* for older data.
                fullName = ExtractInputValuesHelper.ExtractAnswerString(submission, "question_default_full_name", "text")
                    ?? ExtractInputValuesHelper.ExtractAnswerString(submission, "core_full_name", "text");
                gender = ExtractInputValuesHelper.ExtractAnswerString(submission, "question_default_gender", "radio")
                    ?? ExtractInputValuesHelper.ExtractAnswerString(submission, "core_gender", "radio");
                var dob = ExtractInputValuesHelper.ExtractAnswerDate(submission, "question_default_dob", "date")
                    ?? ExtractInputValuesHelper.ExtractAnswerDate(submission, "core_dob", "date");
                age = dob.HasValue ? ExtractInputValuesHelper.CalculateAge(dob.Value) : null;
                injuryDate = ExtractInputValuesHelper.ExtractAnswerDate(submission, "question_default_injury_date", "date")
                    ?? ExtractInputValuesHelper.ExtractAnswerDate(submission, "core_dob", "date");
            }
        }

        // Chief Complaint now lives in the form (question_default_chief_complaint) per requirements;
        // read it from the submission when possible, otherwise fall back to the legacy pain-map extraction.
        var chiefComplaint = (submission is not null && schema is not null)
            ? ExtractInputValuesHelper.ExtractAnswerString(
                submission,
                ExtractInputValuesHelper.FindQuestionIdByText(schema, "Chief Complaint") ?? "question_default_chief_complaint",
                ExtractInputValuesHelper.GetWrapperKey(schema, "question_default_chief_complaint"))
            : null;
        chiefComplaint = string.IsNullOrWhiteSpace(chiefComplaint)
            ? ExtractInputValuesHelper.ExtractChiefComplaint(intake.PainPointsData)
            : chiefComplaint;

        var injury = ExtractInputValuesHelper.ExtractInjury(intake.PainPointsData);
        var patientCategory = ExtractInputValuesHelper.ExtractPatientCategory(intake.PainPointsData);

        // Patient Type now lives in the form (question_default_patient_type) per requirements.
        // It is a distinct intake attribute (New/Returning/Post-Surgery/Chronic) and is NOT the
        // clinical PatientCategory — read it from the submission, never from the pain map.
        var patientType = (submission is not null && schema is not null)
            ? ExtractInputValuesHelper.ExtractAnswerString(
                submission,
                ExtractInputValuesHelper.FindQuestionIdByText(schema, "Patient Type") ?? "question_default_patient_type",
                ExtractInputValuesHelper.GetWrapperKey(schema, "question_default_patient_type"))
            : null;
        patientType = string.IsNullOrWhiteSpace(patientType) ? null : patientType;

        // Provide sensible defaults so NO field shows as empty in initial report summary
        fullName = string.IsNullOrWhiteSpace(fullName) ? "Patient" : fullName;
        gender = string.IsNullOrWhiteSpace(gender) ? "Not Specified" : gender;
        chiefComplaint = string.IsNullOrWhiteSpace(chiefComplaint) ? "Pre-visit intake assessment & general physiotherapy evaluation." : chiefComplaint;
        injury = string.IsNullOrWhiteSpace(injury) ? "Pre-visit intake pain assessment." : injury;

        return Result.Success(new PatientIntakeSummaryResponse(fullName, gender, age, chiefComplaint, injury, injuryDate, patientCategory, patientType));
    }

    private async Task<DTOs.DynamicForms.DynamicFormSchemaDto?> LoadFormSchemaAsync(Guid formSchemaId)
    {
        var schemaEntity = await _context.PatientFormSchemas
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == formSchemaId);

        if (schemaEntity is null) return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<DTOs.DynamicForms.DynamicFormSchemaDto>(
                schemaEntity.SchemaJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    public async Task<Result<string?>> GetPatientFreeTimeTextAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var intake = await _context.PreVisitIntakes
            .Where(i => i.ConvertedToPatientId == patientId)
            .OrderByDescending(i => i.ReviewedAt) // most recent conversion, in case of any edge case with more than one
            .FirstOrDefaultAsync(cancellationToken);

        if (intake is null)
            return Result.Success<string?>(null); // no intake on file — not an error

        var submission = ExtractInputValuesHelper.DeserializeSubmissionJson(intake.FormSubmissionData);

        if (submission is null)
            return Result.Success<string?>(null); // malformed/empty submission — same non-error fallback

        var freeTimeText = ExtractInputValuesHelper.ExtractAnswerString(submission, "question_personal_free_time", "text")
            ?? ExtractInputValuesHelper.ExtractAnswerString(submission, "question_default_free_time", "text");

        return Result.Success(freeTimeText);
    }
}