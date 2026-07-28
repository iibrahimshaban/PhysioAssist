using PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;

namespace PhysioAssist.Api.Modules.Intake.Helpers;

/// <summary>
/// Builds the starter DynamicFormSchemaDto seeded for every doctor on first access.
/// Mandatory field IDs follow the "question_default_" convention mandated by the
/// Pre-Visit Intake requirements doc, and every mandatory field is IsLocked = true so
/// doctors/receptionists can never delete it (other modules depend on these IDs).
/// Optional/recommended fields are not locked.
/// </summary>
public static class DefaultIntakeSchemaTemplate
{
    public static DynamicFormSchemaDto Build()
    {
        return new DynamicFormSchemaDto
        {
            SchemaVersion = 1,
            Sections = new List<FormSectionDto>
            {
                // ── Locked mandatory core fields ──
                new()
                {
                    SectionId = "section_core_fields",
                    Title = "Required Patient Information",
                    Order = 1,
                    Groups = new List<FormGroupDto>
                    {
                        new()
                        {
                            GroupId = "group_core_fields",
                            Title = "Patient Details",
                            Order = 1,
                            Questions = new List<FormQuestionDto>
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
                                    Placeholder = "e.g. Weekdays after 5pm, weekends anytime",
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
                            },
                        },
                    },
                },

                // ── Medical information (mandatory chief complaint + injury date, recommended others) ──
                new()
                {
                    SectionId = "section_medical_info",
                    Title = "Medical Information",
                    Order = 2,
                    Groups = new List<FormGroupDto>
                    {
                        new()
                        {
                            GroupId = "group_medical_info",
                            Title = "Medical Details",
                            Order = 1,
                            Questions = new List<FormQuestionDto>
                            {
                                new()
                                {
                                    QuestionId = "question_default_chief_complaint",
                                    Text = "Chief Complaint",
                                    Type = "textarea",
                                    Required = true,
                                    IsLocked = true,
                                    Order = 1,
                                    Placeholder = "Primary reason for the visit (moved here from the pain map)",
                                },
                                new()
                                {
                                    QuestionId = "question_default_injury_date",
                                    Text = "Injury Date",
                                    Type = "date",
                                    Required = true,
                                    IsLocked = true,
                                    Order = 2,
                                },
                                new()
                                {
                                    QuestionId = "question_default_patient_type",
                                    Text = "Patient Type",
                                    Type = "select",
                                    Required = true,
                                    IsLocked = true,
                                    Order = 3,
                                    Options = new List<string>
                                    {
                                        "New Patient",
                                        "Returning Patient",
                                        "Post-Surgery",
                                        "Chronic Condition",
                                    },
                                },
                                new()
                                {
                                    QuestionId = "question_medical_previous_injuries",
                                    Text = "Previous Injuries",
                                    Type = "text",
                                    Required = false,
                                    Order = 4,
                                    Placeholder = "e.g. None, or describe prior injuries",
                                },
                                new()
                                {
                                    QuestionId = "question_medical_notes",
                                    Text = "Notes",
                                    Type = "textarea",
                                    Required = false,
                                    Order = 5,
                                    Placeholder = "e.g. Pain worsens after long sitting.",
                                },
                            },
                        },
                    },
                },

                // ── Clinical Summary (display-only; shown in edit + submission view) ──
                new()
                {
                    SectionId = "section_clinical_summary",
                    Title = "Clinical Summary",
                    Order = 4,
                    Groups = new List<FormGroupDto>
                    {
                        new()
                        {
                            GroupId = "group_clinical_summary",
                            Title = "Summary",
                            Order = 1,
                            Questions = new List<FormQuestionDto>
                            {
                                new()
                                {
                                    QuestionId = "question_clinical_summary",
                                    Text = "Clinical Summary",
                                    Type = "summary",
                                    Required = false,
                                    IsLocked = true,
                                    Order = 1,
                                },
                            },
                        },
                    },
                },
            },
        };
    }
}
