using System.Text.RegularExpressions;

namespace PhysioAssist.Api.Shared.Helpers;

public static class ReportTextFormatter
{
    private const string ExaminationMarker = "=== Examination ===";
    private const string TreatmentPlanMarker = "=== Treatment Plan ===";

    public static string Combine(string? examination, string? treatmentPlan)
    {
        return $"{ExaminationMarker}\n{examination?.Trim()}\n{TreatmentPlanMarker}\n{treatmentPlan?.Trim()}";
    }

    public static (string Examination, string TreatmentPlan) Split(string? reportText)
    {
        if (string.IsNullOrEmpty(reportText))
            return (string.Empty, string.Empty);

        var treatmentIndex = reportText.IndexOf(TreatmentPlanMarker, StringComparison.Ordinal);

        var examinationRaw = treatmentIndex >= 0
            ? reportText[..treatmentIndex]
            : reportText;

        var treatmentRaw = treatmentIndex >= 0
            ? reportText[(treatmentIndex + TreatmentPlanMarker.Length)..]
            : string.Empty;

        var examination = examinationRaw.Replace(ExaminationMarker, string.Empty).Trim();
        var treatmentPlan = treatmentRaw.Trim();

        return (StripStrayEqualsArtifact(examination), treatmentPlan);
    }

    private static string StripStrayEqualsArtifact(string text)
    {
        return Regex.Replace(text, @"(?:\r?\n=+\s*)+$", string.Empty).TrimEnd();
    }
}
