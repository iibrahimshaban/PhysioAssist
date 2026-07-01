using PhysioAssist.Api.Modules.Intake.Errors;
using PhysioAssist.Api.Modules.Intake.Repositories;

namespace PhysioAssist.Api.Modules.Intake.Services;

public class IntakeService(
    IPatientFormSchemaRepository patientFormSchemaRepository,
    IPreVisitIntakeRepository preVisitIntakeRepository) : IIntakeService
{
    private readonly IPatientFormSchemaRepository _patientFormSchemaRepository = patientFormSchemaRepository;
    private readonly IPreVisitIntakeRepository _preVisitIntakeRepository = preVisitIntakeRepository;

    public async Task<Result> EnsureSchemaBelongsToDoctorAsync(Guid schemaId, Guid doctorId, CancellationToken cancellationToken = default)
    {
        var schema = await _patientFormSchemaRepository.GetByIdAsync(schemaId, cancellationToken);

        if (schema is null)
            return Result.Failure(IntakeErrors.SchemaNotFound);

        if (schema.DoctorId != doctorId)
            return Result.Failure(IntakeErrors.UnauthorizedDoctor);

        return Result.Success();
    }

    public async Task<Result> EnsureIntakeBelongsToDoctorAsync(Guid intakeId, Guid doctorId, CancellationToken cancellationToken = default)
    {
        var intake = await _preVisitIntakeRepository.GetByIdAsync(intakeId, cancellationToken);

        if (intake is null)
            return Result.Failure(IntakeErrors.IntakeNotFound);

        if (intake.DoctorId != doctorId)
            return Result.Failure(IntakeErrors.UnauthorizedDoctor);

        return Result.Success();
    }
}
