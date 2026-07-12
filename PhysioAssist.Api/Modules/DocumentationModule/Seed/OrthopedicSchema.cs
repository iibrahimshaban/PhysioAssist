namespace PhysioAssist.Api.Modules.DocumentationModule.Seed;

public static class OrthopedicSchema
{
    public const string Json = """
    {
      "category": "Orthopedic",
      "version": 1,
      "fields": [
        {
          "id": "pain_scale",
          "label": "Pain (0-10)",
          "type": "number",
          "min": 0,
          "max": 10
        },
        {
          "id": "pain_location",
          "label": "Pain Location",
          "type": "text"
        },
        {
          "id": "rom",
          "label": "Range of Motion",
          "type": "repeatable_group",
          "fields": [
            { "id": "joint", "type": "text" },
            { "id": "motion", "type": "text" },
            { "id": "measurement_type", "type": "select", "options": ["AROM", "PROM"] },
            { "id": "degrees_measured", "type": "number" },
            { "id": "degrees_normal", "type": "number" }
          ]
        },
        {
          "id": "strength",
          "label": "Muscle Strength (MMT)",
          "type": "repeatable_group",
          "fields": [
            { "id": "muscle_group", "type": "text" },
            { "id": "side", "type": "select", "options": ["Left", "Right", "Bilateral"] },
            { "id": "mmt_grade", "type": "select", "options": ["0", "1", "2", "3", "4", "5"] }
          ]
        },
        {
          "id": "special_tests",
          "label": "Special Tests",
          "type": "repeatable_group",
          "fields": [
            { "id": "test_name", "type": "text" },
            { "id": "side", "type": "select", "options": ["Left", "Right", "Bilateral"] },
            { "id": "result", "type": "select", "options": ["Positive", "Negative", "Inconclusive"] }
          ]
        },
        {
          "id": "outcome_measure_type",
          "label": "Standardized Outcome Measure Used",
          "type": "select",
          "options": ["Oswestry Disability Index (ODI)", "Lower Extremity Functional Scale (LEFS)", "Timed Up and Go (TUG)", "None"]
        },
        {
          "id": "outcome_measure_score",
          "label": "Outcome Measure Score",
          "type": "number",
          "showIf": { "outcome_measure_type": ["Oswestry Disability Index (ODI)", "Lower Extremity Functional Scale (LEFS)", "Timed Up and Go (TUG)"] }
        },
        {
          "id": "gait_posture_observation",
          "label": "Gait / Posture Observation",
          "type": "text"
        },
        {
          "id": "assistive_device",
          "label": "Assistive Device Used",
          "type": "text"
        }
      ]
    }
    """;
}
