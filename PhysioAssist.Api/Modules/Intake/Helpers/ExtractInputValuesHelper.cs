using PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;
using System.Text.Json;

namespace PhysioAssist.Api.Modules.Intake.Helpers;

public static class ExtractInputValuesHelper
{
    public static readonly JsonSerializerOptions SubmissionJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Deserializes a stored FormSubmissionData JSON string into a DynamicFormSubmissionDto.
    /// Single source of truth for this — both IntakeService and IntakeQueryService use it.
    /// </summary>
    public static DynamicFormSubmissionDto? DeserializeSubmissionJson(string submissionJson)
    {
        try
        {
            return JsonSerializer.Deserialize<DynamicFormSubmissionDto>(submissionJson, SubmissionJsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static SubmissionAnswerDto? FindAnswer(DynamicFormSubmissionDto submission, string questionId)
    {
        foreach (var section in submission.Sections)
            foreach (var group in section.Groups)
                foreach (var answer in group.Answers)
                    if (answer.QuestionId == questionId)
                        return answer;

        return null;
    }

    public static string? ExtractAnswerString(DynamicFormSubmissionDto submission, string questionId, string wrapperKey)
    {
        var answer = FindAnswer(submission, questionId);
        if (answer?.Value is not JsonElement element)
            return null;

        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(wrapperKey, out var wrapped))
            return wrapped.ValueKind == JsonValueKind.String ? wrapped.GetString() : wrapped.ToString();

        return element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
    }

    public static DateTime? ExtractAnswerDate(DynamicFormSubmissionDto submission, string questionId, string wrapperKey)
    {
        var raw = ExtractAnswerString(submission, questionId, wrapperKey);
        return DateTime.TryParse(raw, out var date) ? date : null;
    }

    public static string? ExtractPatientNameSafe(string formSubmissionData, DynamicFormSchemaDto? schema = null)
    {
        var submission = DeserializeSubmissionJson(formSubmissionData);
        if (submission is null)
            return null;

        // If we have the schema, look up the question ID dynamically by matching the question text.
        // This handles customized forms where question IDs differ from the defaults.
        if (schema is not null)
        {
            var questionId = FindQuestionIdByText(schema, "Full Name")
                          ?? FindQuestionIdByText(schema, "Name");
            if (questionId is not null)
            {
                var wrapperKey = GetWrapperKey(schema, questionId);
                return ExtractAnswerString(submission, questionId, wrapperKey);
            }
        }

        // Fallback: try the canonical question_default_full_name id first (per requirements),
        // then the legacy core_full_name id (older seeded submissions).
        return ExtractAnswerString(submission, "question_default_full_name", "text")
            ?? ExtractAnswerString(submission, "core_full_name", "text");
    }

    public static int CountPainRegions(string? painPointsData)
    {
        if (string.IsNullOrWhiteSpace(painPointsData))
            return 0;

        try
        {
            using var doc = JsonDocument.Parse(painPointsData);
            if (doc.RootElement.TryGetProperty("regions", out var regions) && regions.ValueKind == JsonValueKind.Array)
                return regions.GetArrayLength();
        }
        catch (JsonException)
        {
            // Malformed/legacy PainPointsData — treat as no pain data rather than failing the whole list.
        }

        return 0;
    }

    public static readonly Dictionary<string, PatientCategory> PatientCategoryMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Orthopedic"] = PatientCategory.Orthopedic,
        ["Neurological"] = PatientCategory.Neurological,
        ["Pediatric"] = PatientCategory.Pediatric,
        ["General / Other"] = PatientCategory.GeneralOther,
    };

    public static PatientCategory ExtractPatientCategory(string? painPointsData, PatientCategory fallback = PatientCategory.GeneralOther)
    {
        if (string.IsNullOrWhiteSpace(painPointsData)) return fallback;
        try
        {
            using var doc = JsonDocument.Parse(painPointsData);
            if (doc.RootElement.TryGetProperty("patientCategory", out var cat) && cat.ValueKind == JsonValueKind.String)
            {
                var value = cat.GetString();
                if (!string.IsNullOrWhiteSpace(value) && PatientCategoryMap.TryGetValue(value, out var mapped))
                {
                    return mapped;
                }
            }
        }
        catch (JsonException)
        {
            // fall through to fallback
        }
        return fallback;
    }
    public static string? ExtractChiefComplaint(string? painPointsData)
    {
        if (string.IsNullOrWhiteSpace(painPointsData)) return null;
        try
        {
            using var doc = JsonDocument.Parse(painPointsData);
            return doc.RootElement.TryGetProperty("chiefComplaint", out var cc) && cc.ValueKind == JsonValueKind.String
                ? cc.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string? ExtractInjury(string? painPointsData)
    {
        if (string.IsNullOrWhiteSpace(painPointsData)) return null;
        try
        {
            using var doc = JsonDocument.Parse(painPointsData);
            if (!doc.RootElement.TryGetProperty("regions", out var regions) || regions.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var labels = new List<string>();
            foreach (var region in regions.EnumerateArray())
            {
                if (region.TryGetProperty("labelEn", out var label) && label.ValueKind == JsonValueKind.String)
                {
                    var value = label.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        labels.Add(value);
                    }
                }
            }

            return labels.Count > 0 ? string.Join(", ", labels) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static int CalculateAge(DateTime dob)
    {
        var today = DateTime.UtcNow.Date;
        var age = today.Year - dob.Year;
        if (dob.Date > today.AddYears(-age)) age--;
        return age;
    }

    /// <summary>
    /// Builds the read-only "Clinical Summary" string shown in the intake form's summary
    /// field. Per requirements it is a concise overview: Patient Type · Chief Complaint ·
    /// Injury (+date) · Gender/Age. Derived from the submitted form answers (never the
    /// pain map, except for Injury/ChiefComplaint fallbacks that predate the form fields).
    /// </summary>
    public static string BuildClinicalSummaryText(DynamicFormSubmissionDto submission, DynamicFormSchemaDto? schema, string? painPointsData)
    {
        var parts = new List<string>();

        if (schema is not null)
        {
            var typeQid = FindQuestionIdByText(schema, "Patient Type") ?? "question_default_patient_type";
            var patientType = ExtractAnswerString(submission, typeQid, GetWrapperKey(schema, typeQid));
            if (!string.IsNullOrWhiteSpace(patientType))
                parts.Add($"Patient Type: {patientType}");

            var ccQid = FindQuestionIdByText(schema, "Chief Complaint") ?? "question_default_chief_complaint";
            var chiefComplaint = ExtractAnswerString(submission, ccQid, GetWrapperKey(schema, ccQid));
            if (string.IsNullOrWhiteSpace(chiefComplaint))
                chiefComplaint = ExtractChiefComplaint(painPointsData);
            if (!string.IsNullOrWhiteSpace(chiefComplaint))
                parts.Add($"Chief Complaint: {chiefComplaint}");

            var genderQid = FindQuestionIdByText(schema, "Gender") ?? "question_default_gender";
            var gender = ExtractAnswerString(submission, genderQid, GetWrapperKey(schema, genderQid));
            var dobQid = FindQuestionIdByText(schema, "Date of Birth") ?? "question_default_dob";
            var dob = ExtractAnswerDate(submission, dobQid, GetWrapperKey(schema, dobQid));
            var genderAge = string.IsNullOrWhiteSpace(gender) ? "" : gender;
            if (dob is not null)
                genderAge += (genderAge.Length > 0 ? ", " : "") + $"{CalculateAge(dob.Value)}y";
            if (genderAge.Length > 0)
                parts.Add(genderAge);
        }

        var injury = ExtractInjury(painPointsData);
        if (!string.IsNullOrWhiteSpace(injury))
            parts.Add($"Injury: {injury}");

        return parts.Count > 0 ? string.Join(" · ", parts) : "Pre-visit intake submitted.";
    }

    /// <summary>
    /// Injects the computed clinical summary into a stored FormSubmissionData JSON under the
    /// read-only summary question (dynamically resolved from the schema, fallback
    /// "question_clinical_summary"). The existing renderer displays this value. Pure string
    /// transform — no DB access.
    /// </summary>
    public static string WriteClinicalSummaryIntoSubmissionJson(string formSubmissionData, string summaryText, DynamicFormSchemaDto? schema = null)
    {
        if (string.IsNullOrWhiteSpace(formSubmissionData))
            return formSubmissionData;

        var summaryQuestionId = FindQuestionIdByText(schema, "Clinical Summary")
                             ?? "question_clinical_summary";

        try
        {
            using var doc = JsonDocument.Parse(formSubmissionData);
            var root = doc.RootElement.Clone();

            // Locate the summary answer across all sections/groups, or add it.
            var found = false;
            var output = System.Text.Json.Nodes.JsonNode.Parse(formSubmissionData)!.AsObject();

            foreach (var section in output["sections"]!.AsArray())
            {
                var groups = section["groups"];
                if (groups is null) continue;
                foreach (var group in groups.AsArray())
                {
                    var answers = group["answers"];
                    if (answers is null) continue;
                    foreach (var answer in answers.AsArray())
                    {
                        if (answer["questionId"]?.GetValue<string>() == summaryQuestionId)
                        {
                            answer["value"] = new System.Text.Json.Nodes.JsonObject { ["summary"] = summaryText };
                            found = true;
                        }
                    }
                    if (!found)
                    {
                        answers.AsArray().Add(new System.Text.Json.Nodes.JsonObject
                        {
                            ["questionId"] = summaryQuestionId,
                            ["value"] = new System.Text.Json.Nodes.JsonObject { ["summary"] = summaryText }
                        });
                        found = true;
                    }
                }
                if (found) break;
            }

            return output.ToJsonString();
        }
        catch (JsonException)
        {
            // Malformed submission JSON — leave it untouched rather than failing the save.
            return formSubmissionData;
        }
    }

    public static string? FindQuestionIdByText(DynamicFormSchemaDto? schema, string text)
    {
        if (schema == null) return null;
        foreach (var section in schema.Sections)
        {
            foreach (var group in section.Groups)
            {
                foreach (var question in group.Questions)
                {
                    if (string.Equals(question.Text, text, StringComparison.OrdinalIgnoreCase))
                    {
                        return question.QuestionId;
                    }
                }
            }
        }
        return null;
    }

    public static string GetWrapperKey(DynamicFormSchemaDto? schema, string questionId)
    {
        if (schema == null) return "text";
        foreach (var section in schema.Sections)
        {
            foreach (var group in section.Groups)
            {
                foreach (var question in group.Questions)
                {
                    if (question.QuestionId == questionId)
                    {
                        // The stored answer wrapper key is exactly the question's Type
                        // (e.g. {"radio":"Female"}, {"phone":"010..."}, {"email":"a@b.c"}).
                        // Returning Type directly extracts the inner value for every type,
                        // instead of the previous remap that returned "text"/"value" and
                        // fell through to element.ToString() (raw JSON) for radio/phone/email/etc.
                        return question.Type;
                    }
                }
            }
        }
        return "text";
    }
}