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
    /// The canonical QuestionId prefix used for all core fields.
    /// </summary>
    public const string CoreIdPrefix = "core_";

    /// <summary>
    /// The section title used when injecting core fields into a schema that has no sections.
    /// </summary>
    public const string CoreSectionTitle = "Required Information";

    /// <summary>
    /// The group title used when injecting core fields.
    /// </summary>
    public const string CoreGroupTitle = "Patient Details";

    /// <summary>
    /// Hard Required fields — without these the Initial Report ends up broken or empty.
    /// </summary>
    public static readonly IReadOnlyList<FormQuestionDto> HardRequiredFields = new List<FormQuestionDto>
    {
        new()
        {
            QuestionId = "core_full_name",
            Text = "Full Name",
            Type = "text",
            Required = true,
            IsLocked = true,
            Order = 1,
            Placeholder = "e.g. John Doe",
        },
        new()
        {
            QuestionId = "core_email",
            Text = "Email Address",
            Type = "email",
            Required = true,
            IsLocked = true,
            Order = 2,
            Placeholder = "john@example.com",
        },
        new()
        {
            QuestionId = "core_phone",
            Text = "Phone Number",
            Type = "phone",
            Required = true,
            IsLocked = true,
            Order = 3,
            Placeholder = "(555) 000-0000",
        },
        new()
        {
            QuestionId = "core_gender",
            Text = "Gender",
            Type = "radio",
            Required = true,
            IsLocked = true,
            Order = 4,
            Options = new List<string> { "Male", "Female" },
        },
        new()
        {
            QuestionId = "core_dob",
            Text = "Date of Birth",
            Type = "date",
            Required = true,
            IsLocked = true,
            Order = 5,
        },
        new()
        {
            QuestionId = "core_occupation",
            Text = "Occupation",
            Type = "text",
            Required = true,
            IsLocked = true,
            Order = 6,
            Placeholder = "e.g. Software Engineer",
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
            ["core_full_name"] = new(StringComparer.OrdinalIgnoreCase) { "text" },
            ["core_email"] = new(StringComparer.OrdinalIgnoreCase) { "email", "text" },
            ["core_phone"] = new(StringComparer.OrdinalIgnoreCase) { "phone", "text" },
            ["core_gender"] = new(StringComparer.OrdinalIgnoreCase) { "radio", "select" },
            ["core_dob"] = new(StringComparer.OrdinalIgnoreCase) { "date" },
            ["core_occupation"] = new(StringComparer.OrdinalIgnoreCase) { "text" },
        };
}