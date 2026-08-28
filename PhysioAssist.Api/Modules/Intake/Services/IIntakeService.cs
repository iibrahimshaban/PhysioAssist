using PhysioAssist.Api.Modules.Intake.DTOs.FormSchemas;
using PhysioAssist.Api.Modules.Intake.DTOs.PublicAccess;
using PhysioAssist.Api.Modules.Intake.DTOs.Submissions;

namespace PhysioAssist.Api.Modules.Intake.Services;

public interface IIntakeService
{
    Task<Result> EnsureSchemaBelongsToDoctorAsync(Guid schemaId, Guid clinicId, CancellationToken cancellationToken = default);
    Task<Result> EnsureIntakeBelongsToClinicAsync(Guid intakeId, Guid clinicId, CancellationToken cancellationToken = default);

    // Form Schema Management
    Task<Result<FormSchemaResponse>> CreateFormSchemaAsync(CreateFormSchemaRequest request, Guid clinicId, CancellationToken cancellationToken = default);
    Task<Result<FormSchemaResponse>> UpdateFormSchemaAsync(Guid schemaId, UpdateFormSchemaRequest request, Guid clinicId, CancellationToken cancellationToken = default);
    Task<Result<FormSchemaResponse>> PublishFormSchemaAsync(Guid schemaId, PublishFormSchemaRequest request, Guid clinicId, CancellationToken cancellationToken = default);
    Task<Result<FormSchemaResponse>> GetFormSchemaByIdAsync(Guid schemaId, Guid clinicId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<FormSchemaSummaryResponse>>> GetFormSchemasByClinicAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<Result<FormSchemaResponse>> GetDefaultFormSchemaAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<Result<FormSchemaResponse>> GenerateDefaultFormSchemaAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<Result<FormSchemaResponse>> DuplicateFormSchemaAsync(Guid schemaId, Guid clinicId, CancellationToken cancellationToken = default);
    Task<Result> DeleteFormSchemaAsync(Guid schemaId, Guid clinicId, CancellationToken cancellationToken = default);
    Task<Result> ArchiveFormSchemaAsync(Guid schemaId, Guid clinicId, CancellationToken cancellationToken = default);
    Task<Result> UnarchiveFormSchemaAsync(Guid schemaId, Guid clinicId, CancellationToken cancellationToken = default);
    Task<Result<GenerateIntakeQrLinkResponse>> GenerateIntakeQrLinkAsync(Guid schemaId,
        GenerateIntakeQrLinkRequest request, Guid clinicId, Guid generatedByUserId, CancellationToken cancellationToken = default);

    // Public Anonymous Access
    Task<Result<PublicIntakeFormResponse>> GetPublicFormAsync(string token, CancellationToken cancellationToken = default);
    Task<Result<PublicIntakeSubmissionResponse>> SubmitPublicIntakeAsync(string token, SubmitPreVisitIntakeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <c>true</c> when a patient record with the given (trimmed,
    /// lowercased) email already exists in the system.  Used by the public
    /// anonymous intake form to warn the patient BEFORE they submit (so
    /// duplicate patients / broken conversion flows are avoided).
    /// </summary>
    Task<Result<bool>> CheckEmailAvailabilityAsync(string token, string email, CancellationToken cancellationToken = default);
    Task<Result<bool>> CheckPhoneAvailabilityAsync(string token, string phoneNumber, CancellationToken cancellationToken = default);

    // Intake Review
    Task<Result<IReadOnlyList<PreVisitIntakeResponse>>> GetSubmissionsAsync(Guid clinicId, IntakeStatus? status, CancellationToken cancellationToken = default);
    Task<Result<PreVisitIntakeDetailsResponse>> GetSubmissionDetailsAsync(Guid id, Guid clinicId, CancellationToken cancellationToken = default);
    Task<Result<PreVisitIntakeResponse>> UpdateStatusAsync(Guid id, UpdateIntakeStatusRequest request,Guid doctorId ,Guid clinicId, CancellationToken cancellationToken = default);

    // Intake Conversion
    Task<Result<PreVisitIntakeResponse>> ConvertToPatientAsync(Guid id, ConvertIntakeToPatientRequest request, Guid doctorId, Guid? clinicId ,CancellationToken cancellationToken = default);
}
