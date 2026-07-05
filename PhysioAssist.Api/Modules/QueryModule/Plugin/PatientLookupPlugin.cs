using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace PhysioAssist.Api.Modules.QueryModule.Plugin;

public class PatientLookupPlugin
{
    private readonly IPatientQueryService _patientLookupService;

    public PatientLookupPlugin(IPatientQueryService patientLookupService)
    {
        _patientLookupService = patientLookupService;
    }

    [KernelFunction, Description("Finds patients by name (partial match allowed). Returns each match's ID and full name. Use this first if the doctor's query mentions a specific patient by name, before searching sessions.")]
    public async Task<string> FindPatientsByName(
        [Description("The patient's name or partial name, in English or transliterated form")] string name)
    {
        var patients = await _patientLookupService.FindByNameAsync(name);

        if (patients.Count == 0)
            return "No patients found matching that name.";

        return string.Join("\n", patients.Select(p => $"- {p.FullName} (id: {p.Id})"));
    }
}
