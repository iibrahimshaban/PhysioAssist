namespace PhysioAssist.Api.Modules.DocumentationModule.Seed;

public static class NeurologicalSchema
{
    public const string Json = """
    {
      "category": "Neurological",
      "version": 1,
      "fields": [
        {
          "id": "balance_test_type",
          "label": "Balance Test Used",
          "type": "select",
          "options": ["Berg Balance Scale", "Functional Gait Assessment"],
          "helpText": "Use Berg for static/dynamic sitting or standing balance. Use FGA instead if assessing balance during ambulation."
        },
        {
          "id": "berg_balance_score",
          "label": "Berg Balance Scale Score",
          "type": "number",
          "min": 0,
          "max": 56,
          "showIf": { "balance_test_type": "Berg Balance Scale" }
        },
        {
          "id": "fga_score",
          "label": "Functional Gait Assessment Score",
          "type": "number",
          "min": 0,
          "max": 30,
          "showIf": { "balance_test_type": "Functional Gait Assessment" }
        },
        {
          "id": "gait_speed",
          "label": "Gait Speed (m/s)",
          "type": "number",
          "unit": "m/s"
        },
        {
          "id": "tone_assessment",
          "label": "Muscle Tone (Modified Ashworth Scale)",
          "type": "repeatable_group",
          "fields": [
            { "id": "muscle_group", "type": "text" },
            { "id": "side", "type": "select", "options": ["Left", "Right", "Bilateral"] },
            { "id": "mas_score", "type": "select", "options": ["0", "1", "1+", "2", "3", "4"] }
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
          "id": "sensation",
          "label": "Sensation",
          "type": "repeatable_group",
          "fields": [
            { "id": "region", "type": "text" },
            { "id": "status", "type": "select", "options": ["Intact", "Impaired", "Absent"] }
          ]
        },
        {
          "id": "coordination",
          "label": "Coordination",
          "type": "select",
          "options": ["Normal", "Dysmetria", "Ataxia", "Not tested"]
        },
        {
          "id": "transfers",
          "label": "Transfer Status",
          "type": "select",
          "options": ["Independent", "Modified Independent", "Supervision", "Minimal Assist", "Moderate Assist", "Maximal Assist", "Dependent"]
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
