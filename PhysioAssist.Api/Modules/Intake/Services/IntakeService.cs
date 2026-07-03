using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MapsterMapper;
using PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;
using PhysioAssist.Api.Modules.Intake.DTOs.FormSchemas;
using PhysioAssist.Api.Modules.Intake.DTOs.PublicAccess;
using PhysioAssist.Api.Modules.Intake.Entities;
using PhysioAssist.Api.Modules.Intake.Errors;
using PhysioAssist.Api.Modules.Intake.Repositories;
using PhysioAssist.Api.Shared.Interfaces;
using PhysioAssist.Api.Shared.QR;

namespace PhysioAssist.Api.Modules.Intake.Services;

public class IntakeService(
    IPatientFormSchemaRepository patientFormSchemaRepository,
    IPreVisitIntakeRepository preVisitIntakeRepository,
    IDynamicFormValidationService dynamicFormValidationService,
    IQRService qrService,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IIntakeService
{
    private readonly IPatientFormSchemaRepository _patientFormSchemaRepository = patientFormSchemaRepository;
    private readonly IPreVisitIntakeRepository _preVisitIntakeRepository = preVisitIntakeRepository;
    private readonly IDynamicFormValidationService _dynamicFormValidationService = dynamicFormValidationService;
    private readonly IQRService _qrService = qrService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;

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

    public async Task<Result<FormSchemaResponse>> CreateFormSchemaAsync(CreateFormSchemaRequest request, Guid doctorId, CancellationToken cancellationToken = default)
    {
        var schemaDto = DeserializeSchemaJson(request.SchemaJson);
        if (schemaDto is null)
            return Result.Failure<FormSchemaResponse>(IntakeErrors.InvalidSchema);

        var validationResult = _dynamicFormValidationService.ValidateSchema(schemaDto);
        if (validationResult.IsFailure)
            return Result.Failure<FormSchemaResponse>(validationResult.Error);

        var nameExists = await _patientFormSchemaRepository.ExistsNameForDoctorAsync(doctorId, request.Name, null, cancellationToken);
        if (nameExists)
            return Result.Failure<FormSchemaResponse>(IntakeErrors.SchemaNameDuplicated);

        var schema = _mapper.Map<PatientFormSchema>(request);
        schema.DoctorId = doctorId;
        schema.SchemaHash = ComputeSchemaHash(request.SchemaJson);

        if (request.IsDefault)
        {
            await _patientFormSchemaRepository.UnsetDefaultSchemasAsync(doctorId, cancellationToken);
        }

        await _patientFormSchemaRepository.AddAsync(schema, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        var response = _mapper.Map<FormSchemaResponse>(schema);
        return Result.Success(response);
    }

    public async Task<Result<FormSchemaResponse>> UpdateFormSchemaAsync(Guid schemaId, UpdateFormSchemaRequest request, Guid doctorId, CancellationToken cancellationToken = default)
    {
        var schema = await _patientFormSchemaRepository.GetByIdAsync(schemaId, cancellationToken);
        if (schema is null)
            return Result.Failure<FormSchemaResponse>(IntakeErrors.SchemaNotFound);

        if (schema.DoctorId != doctorId)
            return Result.Failure<FormSchemaResponse>(IntakeErrors.UnauthorizedDoctor);

        var schemaDto = DeserializeSchemaJson(request.SchemaJson);
        if (schemaDto is null)
            return Result.Failure<FormSchemaResponse>(IntakeErrors.InvalidSchema);

        var validationResult = _dynamicFormValidationService.ValidateSchema(schemaDto);
        if (validationResult.IsFailure)
            return Result.Failure<FormSchemaResponse>(validationResult.Error);

        var nameExists = await _patientFormSchemaRepository.ExistsNameForDoctorAsync(doctorId, request.Name, schemaId, cancellationToken);
        if (nameExists)
            return Result.Failure<FormSchemaResponse>(IntakeErrors.SchemaNameDuplicated);

        schema.Name = request.Name;
        schema.Description = request.Description;
        schema.SchemaJson = request.SchemaJson;
        schema.SchemaHash = ComputeSchemaHash(request.SchemaJson);
        schema.Version++;

        if (request.IsDefault && !schema.IsDefault)
        {
            await _patientFormSchemaRepository.UnsetDefaultSchemasAsync(doctorId, cancellationToken);
            schema.IsDefault = true;
        }
        else if (!request.IsDefault && schema.IsDefault)
        {
            schema.IsDefault = false;
        }

        _patientFormSchemaRepository.Update(schema);
        await _unitOfWork.SaveAsync(cancellationToken);

        var response = _mapper.Map<FormSchemaResponse>(schema);
        return Result.Success(response);
    }

    public async Task<Result<FormSchemaResponse>> PublishFormSchemaAsync(Guid schemaId, PublishFormSchemaRequest request, Guid doctorId, CancellationToken cancellationToken = default)
    {
        var schema = await _patientFormSchemaRepository.GetByIdAsync(schemaId, cancellationToken);
        if (schema is null)
            return Result.Failure<FormSchemaResponse>(IntakeErrors.SchemaNotFound);

        if (schema.DoctorId != doctorId)
            return Result.Failure<FormSchemaResponse>(IntakeErrors.UnauthorizedDoctor);

        if (schema.Status == FormSchemaStatus.Published)
        {
            var existingResponse = _mapper.Map<FormSchemaResponse>(schema);
            return Result.Success(existingResponse);
        }

        schema.Status = FormSchemaStatus.Published;
        schema.PublishedAt = DateTime.UtcNow;
        schema.Version++;

        _patientFormSchemaRepository.Update(schema);
        await _unitOfWork.SaveAsync(cancellationToken);

        var response = _mapper.Map<FormSchemaResponse>(schema);
        return Result.Success(response);
    }

    public async Task<Result<FormSchemaResponse>> GetFormSchemaByIdAsync(Guid schemaId, Guid doctorId, CancellationToken cancellationToken = default)
    {
        var schema = await _patientFormSchemaRepository.GetByIdAsync(schemaId, cancellationToken);
        if (schema is null)
            return Result.Failure<FormSchemaResponse>(IntakeErrors.SchemaNotFound);

        if (schema.DoctorId != doctorId)
            return Result.Failure<FormSchemaResponse>(IntakeErrors.UnauthorizedDoctor);

        var response = _mapper.Map<FormSchemaResponse>(schema);
        return Result.Success(response);
    }

    public async Task<Result<IReadOnlyList<FormSchemaSummaryResponse>>> GetFormSchemasByDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        var schemas = await _patientFormSchemaRepository.GetByDoctorAsync(doctorId, cancellationToken);
        var responses = _mapper.Map<IReadOnlyList<FormSchemaSummaryResponse>>(schemas);
        return Result.Success(responses);
    }

    public async Task<Result<FormSchemaResponse>> GetDefaultFormSchemaAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        var schema = await _patientFormSchemaRepository.GetDefaultForDoctorAsync(doctorId, cancellationToken);
        if (schema is null)
            return Result.Failure<FormSchemaResponse>(IntakeErrors.SchemaNotFound);

        var response = _mapper.Map<FormSchemaResponse>(schema);
        return Result.Success(response);
    }

    public async Task<Result<GenerateIntakeQrLinkResponse>> GenerateIntakeQrLinkAsync(Guid schemaId, GenerateIntakeQrLinkRequest request, Guid doctorId, CancellationToken cancellationToken = default)
    {
        var schema = await _patientFormSchemaRepository.GetPublishedByIdAsync(schemaId, cancellationToken);
        if (schema is null)
            return Result.Failure<GenerateIntakeQrLinkResponse>(IntakeErrors.SchemaNotFound);

        if (schema.DoctorId != doctorId)
            return Result.Failure<GenerateIntakeQrLinkResponse>(IntakeErrors.UnauthorizedDoctor);

        var expiry = DateTime.UtcNow.AddHours(request.ExpiryHours);
        var nonce = Guid.NewGuid().ToString("N");

        var payload = new QRTokenPayload
        {
            Purpose = QRTokenPurpose.Intake,
            TargetId = schema.Id,
            Expiry = expiry,
            Nonce = nonce
        };

        var tokenResult = _qrService.GenerateToken(payload);
        if (tokenResult.IsFailure)
            return Result.Failure<GenerateIntakeQrLinkResponse>(tokenResult.Error);

        var response = new GenerateIntakeQrLinkResponse
        {
            Token = tokenResult.Value,
            PublicUrl = tokenResult.Value,
            ExpiresAt = expiry
        };

        return Result.Success(response);
    }

    private static DynamicFormSchemaDto? DeserializeSchemaJson(string schemaJson)
    {
        try
        {
            return JsonSerializer.Deserialize<DynamicFormSchemaDto>(schemaJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ComputeSchemaHash(string schemaJson)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(schemaJson));
        return Convert.ToBase64String(hash)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
