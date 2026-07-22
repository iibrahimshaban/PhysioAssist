using System.Text.Json;
using System.Text.Json.Nodes;

namespace PhysioAssist.Api.Modules.Intake.Helpers;

public static class PainPointsSplitHelper
{
    // Returns raw JSON: { "regions": [...] }
    public static string? ExtractPainPointsJson(string? painPointsJson)
    {
        if (string.IsNullOrWhiteSpace(painPointsJson))
            return null;

        try
        {
            var node = JsonNode.Parse(painPointsJson);
            var regions = node?["regions"];
            if (regions is null)
                return null;

            var result = new JsonObject { ["regions"] = regions.DeepClone() };
            return result.ToJsonString();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // Returns raw JSON: { "chiefComplaint": "...", "patientCategory": "..." }
    public static string? ExtractDoctorInfoJson(string? painPointsJson)
    {
        if (string.IsNullOrWhiteSpace(painPointsJson))
            return null;

        try
        {
            var node = JsonNode.Parse(painPointsJson);
            if (node is null)
                return null;

            var result = new JsonObject
            {
                ["chiefComplaint"] = node["chiefComplaint"]?.DeepClone(),
                ["patientCategory"] = node["patientCategory"]?.DeepClone()
            };
            return result.ToJsonString();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}