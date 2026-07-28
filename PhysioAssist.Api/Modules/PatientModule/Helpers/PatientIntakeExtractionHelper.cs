using System.Text.Json;

namespace PhysioAssist.Api.Modules.PatientModule.Helpers;

/// <summary>
/// Patient module's own copy of the dynamic-form JSON extraction logic.
/// Deliberately independent from Intake.Helpers.ExtractInputValuesHelper —
/// Patient module must not take a compile-time dependency on Intake module's
/// internal DTOs (DynamicFormSubmissionDto, SubmissionAnswerDto), so this walks
/// the raw JsonDocument directly instead of deserializing into Intake's types.
/// </summary>
public static class PatientIntakeExtractionHelper
{
    public static JsonDocument? ParseSubmissionJson(string submissionJson)
    {
        try
        {
            return JsonDocument.Parse(submissionJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryGetPropertyCI(JsonElement element, string name, out JsonElement value)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static JsonElement? FindAnswer(JsonElement root, string questionId)
    {
        if (!TryGetPropertyCI(root, "sections", out var sections) || sections.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var section in sections.EnumerateArray())
        {
            if (!TryGetPropertyCI(section, "groups", out var groups) || groups.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var group in groups.EnumerateArray())
            {
                if (!TryGetPropertyCI(group, "answers", out var answers) || answers.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var answer in answers.EnumerateArray())
                {
                    if (TryGetPropertyCI(answer, "questionId", out var qId)
                        && qId.ValueKind == JsonValueKind.String
                        && string.Equals(qId.GetString(), questionId, StringComparison.OrdinalIgnoreCase)
                        && TryGetPropertyCI(answer, "value", out var value))
                    {
                        return value;
                    }
                }
            }
        }

        return null;
    }

    public static string? ExtractAnswerString(JsonElement root, string questionId, string wrapperKey)
    {
        var value = FindAnswer(root, questionId);
        if (value is not JsonElement element)
            return null;

        if (element.ValueKind == JsonValueKind.Object && TryGetPropertyCI(element, wrapperKey, out var wrapped))
            return wrapped.ValueKind == JsonValueKind.String ? wrapped.GetString() : wrapped.ToString();

        return element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
    }

    public static DateTime? ExtractAnswerDate(JsonElement root, string questionId, string wrapperKey)
    {
        var raw = ExtractAnswerString(root, questionId, wrapperKey);
        return DateTime.TryParse(raw, out var date) ? date : null;
    }

    public static string? ExtractChiefComplaint(string? painPointsData)
    {
        if (string.IsNullOrWhiteSpace(painPointsData)) return null;
        try
        {
            using var doc = JsonDocument.Parse(painPointsData);
            return TryGetPropertyCI(doc.RootElement, "chiefComplaint", out var cc) && cc.ValueKind == JsonValueKind.String
                ? cc.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
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
            if (TryGetPropertyCI(doc.RootElement, "patientCategory", out var cat) && cat.ValueKind == JsonValueKind.String)
            {
                var value = cat.GetString();
                if (!string.IsNullOrWhiteSpace(value) && PatientCategoryMap.TryGetValue(value, out var mapped))
                    return mapped;
            }
        }
        catch (JsonException)
        {
            // fall through to fallback
        }
        return fallback;
    }
}