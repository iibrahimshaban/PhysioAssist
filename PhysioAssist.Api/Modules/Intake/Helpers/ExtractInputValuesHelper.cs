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
    /// NOTE: if IntakeService already has an equivalent helper (likely used inside
    /// SubmitPublicIntakeAsync before calling ValidateSubmissionAgainstSchema), reuse
    /// that one instead of adding a second copy that can drift out of sync.
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

    /// <summary>
    /// Finds an answer by questionId anywhere in the submission's sections/groups.
    /// </summary>
    public static SubmissionAnswerDto? FindAnswer(DynamicFormSubmissionDto submission, string questionId)
    {
        foreach (var section in submission.Sections)
            foreach (var group in section.Groups)
                foreach (var answer in group.Answers)
                    if (answer.QuestionId == questionId)
                        return answer;

        return null;
    }

    /// <summary>
    /// Extracts a string value from an answer, unwrapping the frontend's
    /// { "text": "..." } / { "email": "..." } / { "phone": "..." } / { "date": "..." }
    /// wrapper shape. Falls back to a plain unwrapped value if the wrapper isn't present.
    /// </summary>
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
}
