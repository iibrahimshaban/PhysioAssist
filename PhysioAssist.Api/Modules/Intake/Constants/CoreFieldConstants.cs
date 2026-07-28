using PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;

namespace PhysioAssist.Api.Modules.Intake.Constants;

/// <summary>
/// Defines the set of core (minimum required) fields that must exist in every Intake Form Schema.
/// These fields are auto-injected at schema creation, locked against deletion/modification,
/// and enforced at submission time to guarantee the Initial Report never ends up empty.
/// </summary>
public static class CoreFieldConstants
{
    /// <summary>
    /// The canonical QuestionId prefix used for all core (mandatory) fields.
    /// Per the Pre-Visit Intake requirements, mandatory field IDs use the "question_default_" prefix
    /// so doctors/receptionists see them and they can never be removed.
    /// </summary>
    public const string CoreIdPrefix = "question_default_";

    /// <summary>
    /// The section title used when injecting core fields into a schema that has no sections.
    /// </summary>
    public const string CoreSectionTitle = "Required Patient Information";

    /// <summary>
    /// The group title used when injecting core fields.
    /// </summary>
    public const string CoreGroupTitle = "Patient Details";

    /// <summary>
    /// Hard Required fields — without these the Initial Report ends up broken or empty.
    /// IDs follow the mandated "question_default_" convention (see requirements doc).
    /// </summary>
    public static readonly IReadOnlyList<FormQuestionDto> HardRequiredFields = new List<FormQuestionDto>
    {
        new()
        {
            QuestionId = "question_default_full_name",
            Text = "Full Name",
            Type = "text",
            Required = true,
            IsLocked = true,
            Order = 1,
            Placeholder = "e.g. John Doe",
        },
        new()
        {
            QuestionId = "question_default_email",
            Text = "Email Address",
            Type = "email",
            Required = true,
            IsLocked = true,
            Order = 2,
            Placeholder = "john@example.com",
        },
        new()
        {
            QuestionId = "question_default_phone",
            Text = "Phone Number",
            Type = "phone",
            Required = true,
            IsLocked = true,
            Order = 3,
            Placeholder = "(555) 000-0000",
        },
        new()
        {
            QuestionId = "question_default_free_time",
            Text = "Patient Free Time",
            Type = "text",
            Required = true,
            IsLocked = true,
            Order = 4,
            Placeholder = "e.g. Weekdays after 5pm",
        },
        new()
        {
            QuestionId = "question_default_gender",
            Text = "Gender",
            Type = "radio",
            Required = true,
            IsLocked = true,
            Order = 5,
            Options = new List<string> { "Male", "Female" },
        },
        new()
        {
            QuestionId = "question_default_dob",
            Text = "Date of Birth",
            Type = "date",
            Required = true,
            IsLocked = true,
            Order = 6,
        },
        new()
        {
            QuestionId = "question_default_chief_complaint",
            Text = "Chief Complaint",
            Type = "textarea",
            Required = true,
            IsLocked = true,
            Order = 7,
            Placeholder = "Primary reason for the visit",
        },
        new()
        {
            QuestionId = "question_default_injury_date",
            Text = "Injury Date",
            Type = "date",
            Required = true,
            IsLocked = true,
            Order = 8,
        },
        new()
        {
            QuestionId = "question_default_patient_type",
            Text = "Patient Type",
            Type = "select",
            Required = true,
            IsLocked = true,
            Order = 9,
            Options = new List<string> { "New Patient", "Returning Patient", "Post-Surgery", "Chronic Condition" },
        },
    };

    /// <summary>
    /// The set of core QuestionId values for quick lookup.
    /// </summary>
    public static readonly HashSet<string> CoreQuestionIds = HardRequiredFields
        .Select(f => f.QuestionId)
        .ToHashSet();

    /// <summary>
    /// The set of core field texts for matching against existing schema questions.
    /// </summary>
    public static readonly HashSet<string> CoreFieldTexts = HardRequiredFields
        .Select(f => f.Text)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns true if the given QuestionId is a core field.
    /// </summary>
    public static bool IsCoreField(string questionId) => CoreQuestionIds.Contains(questionId);

    /// <summary>
    /// Returns the core field definition for a given QuestionId, or null.
    /// </summary>
    public static FormQuestionDto? GetCoreField(string questionId)
        => HardRequiredFields.FirstOrDefault(f => f.QuestionId == questionId);

    /// <summary>
    /// Valid question types that are compatible with each core field.
    /// Key: core QuestionId, Value: set of allowed type strings.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, HashSet<string>> AllowedTypesForCoreField =
        new Dictionary<string, HashSet<string>>
        {
            ["question_default_full_name"] = new(StringComparer.OrdinalIgnoreCase) { "text" },
            ["question_default_email"] = new(StringComparer.OrdinalIgnoreCase) { "email", "text" },
            ["question_default_phone"] = new(StringComparer.OrdinalIgnoreCase) { "phone", "text" },
            ["question_default_free_time"] = new(StringComparer.OrdinalIgnoreCase) { "text" },
            ["question_default_gender"] = new(StringComparer.OrdinalIgnoreCase) { "radio", "select" },
            ["question_default_dob"] = new(StringComparer.OrdinalIgnoreCase) { "date" },
            ["question_default_chief_complaint"] = new(StringComparer.OrdinalIgnoreCase) { "textarea", "text" },
            ["question_default_injury_date"] = new(StringComparer.OrdinalIgnoreCase) { "date" },
            ["question_default_patient_type"] = new(StringComparer.OrdinalIgnoreCase) { "select" },
        };
}