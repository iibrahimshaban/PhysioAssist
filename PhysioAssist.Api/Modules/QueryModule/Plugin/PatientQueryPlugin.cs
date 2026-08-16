using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace PhysioAssist.Api.Modules.QueryModule.Plugin
{
    public class PatientQueryPlugin
    {
        private readonly IPatientQueryService _patientQueryService;

        public PatientQueryPlugin(IPatientQueryService patientQueryService)
        {
            _patientQueryService = patientQueryService;
        }

        [KernelFunction, Description(
            "Lists or filters ALL patients belonging to the current doctor using exact structured matching. " +
            "Use for enumeration requests: 'list all my patients', 'which patients have diabetes', 'how many patients have X'. " +
            "This returns a complete, exhaustive list — unlike SearchSessionChunks, which only returns the top few " +
            "most semantically similar clinical records and is NOT guaranteed to include every matching patient.")]
        public async Task<string> ListPatients(
            [Description("Optional diagnosis/condition to filter by, in clinical English. Omit to list all patients.")]
            string? diagnosis = null,
            [Description("Max patients to return")] int topN = 20)
        {
            var result = await _patientQueryService.GetPatientsByDiagnosisAsync(diagnosis, topN);

            if (!result.IsSuccess)
                return diagnosis is null
                    ? "No patients found for this doctor."
                    : $"No patients found with diagnosis matching '{diagnosis}'.";

            return string.Join("\n", result.Value.Select(p => $"- {p.FullName} (id: {p.Id})"));
        }
    }
}
