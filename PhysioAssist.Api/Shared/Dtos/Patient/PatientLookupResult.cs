namespace PhysioAssist.Api.Shared.Dtos.Patient;

public sealed record PatientLookupResult(
    Guid Id, 
    string FullName
    );
