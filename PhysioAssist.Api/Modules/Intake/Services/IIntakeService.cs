using PhysioAssist.Api.Modules.Intake.DTOs.FormSchemas;
using PhysioAssist.Api.Modules.Intake.DTOs.PublicAccess;

namespace PhysioAssist.Api.Modules.Intake.Services;

public interface IIntakeService
{
    Task<Result> EnsureSchemaBelongsToDoctorAsync(Guid schemaId, Guid doctorId, CancellationToken cancellationToken = default);
    Task<Result> EnsureIntakeBelongsToDoctorAsync(Guid intakeId, Guid doctorId, CancellationToken cancellationToken = default);

    // Form Schema Management
    Task<Result<FormSchemaResponse>> CreateFormSchemaAsync(CreateFormSchemaRequest request, Guid doctorId, CancellationToken cancellationToken = default);
    Task<Result<FormSchemaResponse>> UpdateFormSchemaAsync(Guid schemaId, UpdateFormSchemaRequest request, Guid doctorId, CancellationToken cancellationToken = default);
    Task<Result<FormSchemaResponse>> PublishFormSchemaAsync(Guid schemaId, PublishFormSchemaRequest request, Guid doctorId, CancellationToken cancellationToken = default);
    Task<Result<FormSchemaResponse>> GetFormSchemaByIdAsync(Guid schemaId, Guid doctorId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<FormSchemaSummaryResponse>>> GetFormSchemasByDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default);
    Task<Result<FormSchemaResponse>> GetDefaultFormSchemaAsync(Guid doctorId, CancellationToken cancellationToken = default);
    Task<Result<GenerateIntakeQrLinkResponse>> GenerateIntakeQrLinkAsync(Guid schemaId, GenerateIntakeQrLinkRequest request, Guid doctorId, CancellationToken cancellationToken = default);
}
