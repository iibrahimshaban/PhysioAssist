namespace PhysioAssist.Api.Modules.DocumentationModule.Seed;

public static class PediatricSchema
{
    public const string Json = """
    {
      "category": "Pediatric",
      "version": 1,
      "fields": [
        {
          "id": "chronological_age_months",
          "label": "Chronological Age (months)",
          "type": "number"
        },
        {
          "id": "developmental_milestones",
          "label": "Developmental Milestones",
          "type": "repeatable_group",
          "fields": [
            { "id": "milestone", "type": "select", "options": ["Head control", "Rolled over", "Sat independently", "Crawled", "Pulled to stand", "Walked independently", "First words"] },
            { "id": "age_achieved_months", "type": "number" },
            { "id": "status", "type": "select", "options": ["Achieved", "Emerging", "Not yet achieved"] }
          ]
        },
        {
          "id": "standardized_test_type",
          "label": "Standardized Test Used",
          "type": "select",
          "options": ["Gross Motor Function Measure (GMFM)", "Pediatric Evaluation of Disability Inventory (PEDI)", "Ages & Stages Questionnaires (ASQ)", "None"]
        },
        {
          "id": "standardized_test_score",
          "label": "Standardized Test Score",
          "type": "text",
          "showIf": { "standardized_test_type": ["Gross Motor Function Measure (GMFM)", "Pediatric Evaluation of Disability Inventory (PEDI)", "Ages & Stages Questionnaires (ASQ)"] }
        },
        {
          "id": "tone_assessment",
          "label": "Muscle Tone",
          "type": "repeatable_group",
          "fields": [
            { "id": "muscle_group", "type": "text" },
            { "id": "side", "type": "select", "options": ["Left", "Right", "Bilateral"] },
            { "id": "tone_description", "type": "select", "options": ["Normal", "Hypotonic", "Hypertonic"] }
          ]
        },
        {
          "id": "postural_control",
          "label": "Postural Control Observation",
          "type": "text"
        },
        {
          "id": "functional_mobility",
          "label": "Functional Mobility",
          "type": "select",
          "options": ["Independent", "Requires Assist", "Uses Assistive Device", "Non-ambulatory"]
        },
        {
          "id": "parent_reported_concerns",
          "label": "Parent/Caregiver-Reported Concerns",
          "type": "text"
        }
      ]
    }
    """;
}
