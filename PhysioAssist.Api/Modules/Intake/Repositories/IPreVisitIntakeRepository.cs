using PhysioAssist.Api.Modules.Intake.Entities;

namespace PhysioAssist.Api.Modules.Intake.Repositories;

public interface IPreVisitIntakeRepository
{
    Task AddAsync(PreVisitIntake intake, CancellationToken cancellationToken = default);
    Task<PreVisitIntake?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PreVisitIntake?> GetDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PreVisitIntake>> GetByDoctorAsync(Guid doctorId, IntakeStatus? status = null, CancellationToken cancellationToken = default);
    Task<PreVisitIntake?> GetByAccessTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<bool> ExistsConvertedAsync(Guid intakeId, CancellationToken cancellationToken = default);
    void Update(PreVisitIntake intake);
}
