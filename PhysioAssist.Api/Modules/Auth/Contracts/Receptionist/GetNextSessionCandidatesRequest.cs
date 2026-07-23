namespace PhysioAssist.Api.Modules.Auth.Contracts.Receptionist;

public class GetNextSessionCandidatesRequest
{
    public string? PatientFreeTimeOverride { get; init; }
    public bool PersistFreeTimeOverride { get; init; } = false;
}
