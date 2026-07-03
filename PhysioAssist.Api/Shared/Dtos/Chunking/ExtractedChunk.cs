using System.ComponentModel;

namespace PhysioAssist.Api.Shared.Dtos.Chunking;

public sealed record ExtractedChunk(
    [property: Description("Short name(s) of the exercise/technique/treatment type covered by this record, e.g. 'core و lower limb strength exercises'.")]
    string Recommendations,

    [property: Description("How the recommendation was carried out — specifics, sets/reps if mentioned, body area, technique details, exactly as the doctor described.")]
    string RecommendationDetails,

    [property: Description("How the patient responded or tolerated the treatment, ONLY if explicitly mentioned by the doctor. Null if not discussed.")]
    string? PatientResponse,

    [property: Description("What the doctor wants to target or continue in the next session.")]
    string NextSessionFocus,

    [property: Description("The medical diagnosis/condition and relevant stage, as a short clear phrase — e.g. 'hemiplegia, late-stage recovery, history of paralysis'. Not a dumping ground — just the diagnosis.")]
    string Diagnosis,

    [property: Description("Anything else clinically relevant the doctor mentioned that doesn't fit the fields above — including things explicitly avoided/excluded and why, compensatory mechanisms or reasoning explanations, incidental clinical history, or logistics. Null only if truly nothing else was said.")]
    string? Notes);
