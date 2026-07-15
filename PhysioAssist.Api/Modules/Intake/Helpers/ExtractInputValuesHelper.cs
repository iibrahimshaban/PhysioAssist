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

    public static string? ExtractPatientNameSafe(string formSubmissionData)
    {
        var submission = DeserializeSubmissionJson(formSubmissionData);
        return submission is null
            ? null
            : ExtractAnswerString(submission, "question_default_full_name", "text");
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
}