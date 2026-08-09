using PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;

namespace PhysioAssist.Api.Modules.Intake.Helpers;

/// <summary>
/// Builds the starter DynamicFormSchemaDto seeded for every doctor on first access.
/// Mandatory field IDs follow the "question_default_" convention mandated by the
/// Pre-Visit Intake requirements doc, and every mandatory field is IsLocked = true so
/// doctors/receptionists can never delete it (other modules depend on these IDs).
/// Optional/recommended fields are not locked.
///
/// All core fields live under a single locked section ("section_core_fields") split into
/// three locked groups — Patient Details, Medical Information, Clinical Summary — mirroring
/// the fixed structure the schema-builder frontend renders for the core section. Group IDs
/// ("group_core_fields", "group_medical_information", "group_clinical_summary") must match
/// the frontend's hardcoded constants exactly, otherwise the frontend's load-time migration
/// will treat these as legacy groups and spin up duplicate empty ones.
///
/// Question order within each group follows patient-facing UX flow, not insertion order:
/// identity fields first, then contact info, then demographic/lifestyle extras, ending
/// with the lowest-relevance "how did you know us?" marketing question.
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
                new()
                {
                    SectionId = "section_core_fields",
                    Title = "Required Patient Information",
                    Order = 1,
                    Groups = new List<FormGroupDto>
                    {
                        // ── Patient Details ──
                        // Flow: who am I -> how do you reach me -> where I live / what I do ->
                        // lifestyle extras -> scheduling -> referral source (last, lowest relevance to patient).
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
                                    QuestionId = "question_default_gender",
                                    Text = "Gender",
                                    Type = "radio",
                                    Required = true,
                                    IsLocked = true,
                                    Order = 2,
                                    Options = new List<string> { "Male", "Female" },
                                },
                                new()
                                {
                                    QuestionId = "question_default_dob",
                                    Text = "Date of Birth",
                                    Type = "date",
                                    Required = true,
                                    IsLocked = true,
                                    Order = 3,
                                },
                                new()
                                {
                                    QuestionId = "question_default_phone",
                                    Text = "Phone Number",
                                    Type = "phone",
                                    Required = true,
                                    IsLocked = true,
                                    Order = 4,
                                    Placeholder = "(555) 000-0000",
                                },
                                new()
                                {
                                    QuestionId = "question_default_email",
                                    Text = "Email Address",
                                    Type = "email",
                                    Required = true,
                                    IsLocked = true,
                                    Order = 5,
                                    Placeholder = "john@example.com",
                                },
                                new()
                                {
                                    QuestionId = "question_default_address",
                                    Text = "Address / City",
                                    Type = "text",
                                    Required = false,
                                    Order = 6,
                                    Placeholder = "e.g. Giza, Egypt",
                                },
                                new()
                                {
                                    QuestionId = "question_default_job",
                                    Text = "Job / Occupation",
                                    Type = "text",
                                    Required = false,
                                    Order = 7,
                                    Placeholder = "e.g. Software Engineer",
                                },
                                new()
                                {
                                    QuestionId = "question_default_marital_status",
                                    Text = "Married",
                                    Type = "boolean",
                                    Required = false,
                                    Order = 8
                                },
                                new()
                                {
                                    QuestionId = "question_default_free_time",
                                    Text = "Patient Free Time",
                                    Type = "text",
                                    Required = true,
                                    IsLocked = true,
                                    Order = 9,
                                    Placeholder = "e.g. Weekdays after 5pm, weekends anytime",
                                },
                                new()
                                {
                                    QuestionId = "question_default_referral_source",
                                    Text = "How did you know us?",
                                    Type = "multiselect",
                                    Required = true,
                                    Order = 10,
                                    Options = new List<string>
                                    {
                                        "Social Media",
                                        "Friend or Family",
                                        "Google Search",
                                        "Doctor Referral",
                                        "Advertisement",
                                        "Other"
                                    },
                                }
                            },
                        },

                        // ── Medical Information ──
                        // Flow: why I'm here -> when it happened -> relevant history -> anything else.
                        new()
                        {
                            GroupId = "group_medical_information",
                            Title = "Medical Information",
                            Order = 2,
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
                                    QuestionId = "question_medical_previous_injuries",
                                    Text = "Previous Injuries",
                                    Type = "text",
                                    Required = false,
                                    Order = 3,
                                    Placeholder = "e.g. None, or describe prior injuries",
                                },
                                new()
                                {
                                    QuestionId = "question_medical_notes",
                                    Text = "Notes",
                                    Type = "textarea",
                                    Required = false,
                                    Order = 4,
                                    Placeholder = "e.g. Pain worsens after long sitting.",
                                },
                            },
                        },

                        // ── Clinical Summary ──
                        // Flow: quick classification tap first, open-ended narrative second.
                        new()
                        {
                            GroupId = "group_clinical_summary",
                            Title = "Clinical Summary",
                            Order = 3,
                            HiddenFromPatient = true,
                            Questions = new List<FormQuestionDto>
                            {
                                new()
                                {
                                    QuestionId = "question_default_patient_type",
                                    Text = "Patient Type",
                                    Type = "select",
                                    Required = true,
                                    IsLocked = true,
                                    Order = 1,
                                    Options = new List<string>
                                    {
                                        "Orthopedic",
                                        "Neurological",
                                        "Pediatric",
                                        "GeneralOther",
                                    },
                                },
                                new()
                                {
                                    QuestionId = "question_clinical_summary",
                                    Text = "Clinical Summary",
                                    Type = "textarea",
                                    Required = false,
                                    IsLocked = true,
                                    Order = 2,
                                },
                            },
                        },
                    },
                },
            },
        };
    }
}