namespace PhysioAssist.Api.Shared.Interfaces.Exposed;

public interface IClinicDoctorResolver
{
    Task<IReadOnlyList<Guid>> GetDoctorIdsForClinicAsync(
        Guid clinicId, CancellationToken cancellationToken = default);
}
