using PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;

namespace PhysioAssist.Api.Modules.Intake.Services;

/// <summary>
/// Builds the starter DynamicFormSchemaDto seeded for every doctor on email confirmation.
/// Doctors can freely edit/reorder/remove these fields afterward through the normal
/// UpdateFormSchemaAsync flow — this is just the initial state.
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
                    SectionId = "section_default_personal_info",
                    Title = "Personal Information",
                    Order = 1,
                    Groups = new List<FormGroupDto>
                    {
                        new()
                        {
                            GroupId = "group_default_basic_details",
                            Title = "Basic Details",
                            Order = 1,
                            Questions = new List<FormQuestionDto>
                            {
                                new()
                                {
                                    QuestionId = "question_default_full_name",
                                    Text = "Full Name",
                                    Type = "text",
                                    Required = true,
                                    Order = 1,
                                    Placeholder = "e.g. John Doe",
                                },
                                new()
                                {
                                    QuestionId = "question_default_dob",
                                    Text = "Date of Birth",
                                    Type = "date",
                                    Required = false,
                                    Order = 2,
                                },
                                new()
                                {
                                    QuestionId = "question_default_gender",
                                    Text = "Gender",
                                    Type = "radio",
                                    Required = false,
                                    Order = 3,
                                    Options = new List<string> { "Male", "Female" },
                                },
                                new()
                                {
                                    QuestionId = "question_default_email",
                                    Text = "Email Address",
                                    Type = "email",
                                    Required = false,
                                    Order = 4,
                                    Placeholder = "john@example.com",
                                },
                                new()
                                {
                                    QuestionId = "question_default_phone",
                                    Text = "Phone Number",
                                    Type = "phone",
                                    Required = false,
                                    Order = 5,
                                    Placeholder = "(555) 000-0000",
                                },
                                new()
                                {
                                    QuestionId = "question_default_job",
                                    Text = "Job / Occupation",
                                    Type = "text",
                                    Required = false,
                                    Order = 6,
                                    Placeholder = "e.g. Software Engineer",
                                },
                                new()
                                {
                                    QuestionId = "question_default_address",
                                    Text = "Address / City",
                                    Type = "text",
                                    Required = false,
                                    Order = 7,
                                    Placeholder = "e.g. Giza, Egypt",
                                },
                                new()
                                {
                                    QuestionId = "question_default_free_time",
                                    Text = "Free Time for Scheduling",
                                    Type = "text",
                                    Required = false,
                                    Order = 8,
                                    Placeholder = "e.g. Weekdays after 5pm, weekends anytime",
                                },
                                new()
                                {
                                    QuestionId = "question_default_marital_status",
                                    Text = "Married",
                                    Type = "boolean",
                                    Required = false,
                                    Order = 9,
                                },
                                new()
                                {
                                    QuestionId = "question_default_referral_source",
                                    Text = "How did you know us?",
                                    Type = "multiselect",
                                    Required = false,
                                    Order = 10,
                                    Options = new List<string>
                                    {
                                        "Social Media",
                                        "Friend or Family",
                                        "Google Search",
                                        "Doctor Referral",
                                        "Advertisement",
                                        "Other",
                                    },
                                },
                            },
                        },
                    },
                },
                new()
                {
                    SectionId = "section_default_medical_history",
                    Title = "", // no big heading — "Medical History" shows as the group caption below, matching the reference layout
                    Order = 2,
                    Groups = new List<FormGroupDto>
                    {
                        new()
                        {
                            GroupId = "group_default_medical_history",
                            Title = "Medical History",
                            Order = 1,
                            Questions = new List<FormQuestionDto>
                            {
                                new()
                                {
                                    QuestionId = "question_default_injury_date",
                                    Text = "Injury Date",
                                    Type = "date",
                                    Required = false,
                                    Order = 1,
                                },
                                new()
                                {
                                    QuestionId = "question_default_previous_injuries",
                                    Text = "Previous Injuries",
                                    Type = "text",
                                    Required = false,
                                    Order = 2,
                                    Placeholder = "e.g. None, or describe prior injuries",
                                },
                                new()
                                {
                                    QuestionId = "question_default_medical_notes",
                                    Text = "Notes",
                                    Type = "textarea",
                                    Required = false,
                                    Order = 3,
                                    Placeholder = "e.g. Pain worsens after long sitting. Sharp on standing up.",
                                },
                            },
                        },
                    },
                },
            },
        };
    }
}