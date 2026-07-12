using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MapsterMapper;
using PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;
using PhysioAssist.Api.Modules.Intake.DTOs.FormSchemas;
using PhysioAssist.Api.Modules.Intake.DTOs.PublicAccess;
using PhysioAssist.Api.Modules.Intake.DTOs.Submissions;
using PhysioAssist.Api.Modules.Intake.Entities;
using PhysioAssist.Api.Modules.Intake.Errors;
using PhysioAssist.Api.Modules.Intake.Helpers;
using PhysioAssist.Api.Modules.Intake.Repositories;
using PhysioAssist.Api.Modules.PatientModule.Entities;
using PhysioAssist.Api.Modules.PatientModule.Repositories;
using PhysioAssist.Api.Shared.Consts;
using PhysioAssist.Api.Shared.QR;

namespace PhysioAssist.Api.Modules.Intake.Services;

public class IntakeService(
    IPatientFormSchemaRepository patientFormSchemaRepository,
    IPreVisitIntakeRepository preVisitIntakeRepository,
    IDynamicFormValidationService dynamicFormValidationService,
    IQRService qrService,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ILogger<IntakeService> logger,
    IPatientRepo patientRepo,
    IDoctorPatientRepo doctorPatientRepo) : IIntakeService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HashSet<(IntakeStatus, IntakeStatus)> _allowedStatusTransitions = new()
    {
        // Pending can go to review states or be rejected/expired
        (IntakeStatus.Pending, IntakeStatus.InReview),
        (IntakeStatus.Pending, IntakeStatus.Approved),
        (IntakeStatus.Pending, IntakeStatus.Rejected),
        (IntakeStatus.Pending, IntakeStatus.Expired),

        // Submitted can go to review states or be rejected/expired
        (IntakeStatus.Submitted, IntakeStatus.InReview),
        (IntakeStatus.Submitted, IntakeStatus.Approved),
        (IntakeStatus.Submitted, IntakeStatus.Rejected),
        (IntakeStatus.Submitted, IntakeStatus.Expired),

        // InReview can be approved or rejected (terminal review)
        (IntakeStatus.InReview, IntakeStatus.Approved),
        (IntakeStatus.InReview, IntakeStatus.Rejected),
        (IntakeStatus.InReview, IntakeStatus.Expired),

        // Approved can be converted (via separate endpoint), rejected, or expired
        (IntakeStatus.Approved, IntakeStatus.Rejected),
        (IntakeStatus.Approved, IntakeStatus.Expired),

        // Rejected can be re-opened for review or re-approved
        (IntakeStatus.Rejected, IntakeStatus.InReview),
        (IntakeStatus.Rejected, IntakeStatus.Approved),
        (IntakeStatus.Rejected, IntakeStatus.Expired),

        // Converted and Expired are terminal - no transitions allowed
    };

    private readonly IPatientFormSchemaRepository _patientFormSchemaRepository = patientFormSchemaRepository;
    private readonly IPreVisitIntakeRepository _preVisitIntakeRepository = preVisitIntakeRepository;
    private readonly IDynamicFormValidationService _dynamicFormValidationService = dynamicFormValidationService;
    private readonly IQRService _qrService = qrService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<IntakeService> _logger = logger;
    private readonly IPatientRepo _patientRepo = patientRepo;
    private readonly IDoctorPatientRepo _doctorPatientRepo = doctorPatientRepo;

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
        schema.CreatedById = DefaultUsers.UserId;
        schema.CreatedAt = DateTime.UtcNow;

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

    public async Task<Result<PublicIntakeFormResponse>> GetPublicFormAsync(string token, CancellationToken cancellationToken = default)
    {
        var tokenValidationResult = _qrService.ValidateToken(token, QRTokenPurpose.Intake);
        if (tokenValidationResult.IsFailure)
            return Result.Failure<PublicIntakeFormResponse>(tokenValidationResult.Error);

        var payload = tokenValidationResult.Value;
        var schema = await _patientFormSchemaRepository.GetPublishedByIdAsync(payload.TargetId, cancellationToken);
        if (schema is null)
            return Result.Failure<PublicIntakeFormResponse>(IntakeErrors.SchemaNotFound);

        var response = _mapper.Map<PublicIntakeFormResponse>(schema);
        return Result.Success(response);
    }

    public async Task<Result<PublicIntakeSubmissionResponse>> SubmitPublicIntakeAsync(string token, SubmitPreVisitIntakeRequest request, CancellationToken cancellationToken = default)
    {
        var tokenValidationResult = _qrService.ValidateToken(token, QRTokenPurpose.Intake);
        if (tokenValidationResult.IsFailure)
            return Result.Failure<PublicIntakeSubmissionResponse>(tokenValidationResult.Error);

        var payload = tokenValidationResult.Value;
        var schema = await _patientFormSchemaRepository.GetPublishedByIdAsync(payload.TargetId, cancellationToken);
        if (schema is null)
            return Result.Failure<PublicIntakeSubmissionResponse>(IntakeErrors.SchemaNotFound);

        var schemaDto = DeserializeSchemaJson(schema.SchemaJson);
        if (schemaDto is null)
            return Result.Failure<PublicIntakeSubmissionResponse>(IntakeErrors.InvalidSchema);

        var submissionDto = DeserializeSubmissionJson(request.FormSubmissionData);
        if (submissionDto is null)
            return Result.Failure<PublicIntakeSubmissionResponse>(IntakeErrors.InvalidSubmission);

        var validationResult = _dynamicFormValidationService.ValidateSubmissionAgainstSchema(schemaDto, submissionDto);
        if (validationResult.IsFailure)
            return Result.Failure<PublicIntakeSubmissionResponse>(validationResult.Error);

        var intake = _mapper.Map<PreVisitIntake>(request);
        intake.DoctorId = schema.DoctorId;
        intake.FormSchemaId = schema.Id;
        intake.FormSchemaVersion = schema.Version;
        intake.Status = IntakeStatus.Pending;
        intake.SubmittedAt = DateTime.UtcNow;
        intake.AccessTokenHash = null;
        intake.ExpiresAt = null;

        await _preVisitIntakeRepository.AddAsync(intake, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        var response = _mapper.Map<PublicIntakeSubmissionResponse>(intake);
        response = response with { Message = "Your intake form has been submitted successfully." };

        return Result.Success(response);
    }

    public async Task<Result<IReadOnlyList<PreVisitIntakeResponse>>> GetSubmissionsAsync(
    Guid doctorId, IntakeStatus? status, CancellationToken cancellationToken = default)
    {
        var intakes = await _preVisitIntakeRepository.GetByDoctorAsync(doctorId, status, cancellationToken);

        var responses = intakes.Select(intake =>
        {
            var response = _mapper.Map<PreVisitIntakeResponse>(intake);
            return response with
            {
                PatientName = ExtractInputValuesHelper.ExtractPatientNameSafe(intake.FormSubmissionData),
                PainRegionCount = ExtractInputValuesHelper.CountPainRegions(intake.PainPointsData)
            };
        }).ToList();

        return Result.Success<IReadOnlyList<PreVisitIntakeResponse>>(responses);
    }

    public async Task<Result<PreVisitIntakeDetailsResponse>> GetSubmissionDetailsAsync(Guid id, Guid doctorId, CancellationToken cancellationToken = default)
    {
        var intake = await _preVisitIntakeRepository.GetDetailsByIdAsync(id, cancellationToken);
        if (intake is null)
            return Result.Failure<PreVisitIntakeDetailsResponse>(IntakeErrors.IntakeNotFound);

        if (intake.DoctorId != doctorId)
            return Result.Failure<PreVisitIntakeDetailsResponse>(IntakeErrors.UnauthorizedDoctor);

        var response = _mapper.Map<PreVisitIntakeDetailsResponse>(intake);
        return Result.Success(response);
    }

    public async Task<Result<PreVisitIntakeResponse>> UpdateStatusAsync(Guid id, UpdateIntakeStatusRequest request, Guid doctorId, CancellationToken cancellationToken = default)
    {
        var intake = await _preVisitIntakeRepository.GetByIdAsync(id, cancellationToken);
        if (intake is null)
            return Result.Failure<PreVisitIntakeResponse>(IntakeErrors.IntakeNotFound);

        if (intake.DoctorId != doctorId)
            return Result.Failure<PreVisitIntakeResponse>(IntakeErrors.UnauthorizedDoctor);

        if (intake.Status == request.NewStatus)
            return Result.Success(_mapper.Map<PreVisitIntakeResponse>(intake));

        if (!_allowedStatusTransitions.Contains((intake.Status, request.NewStatus)))
        {
            _logger.LogWarning("Invalid status transition attempted: {CurrentStatus} -> {RequestedStatus} for intake {IntakeId} by doctor {DoctorId}",
                intake.Status, request.NewStatus, id, doctorId);
            return Result.Failure<PreVisitIntakeResponse>(IntakeErrors.InvalidStatusTransition);
        }

        var oldStatus = intake.Status;
        intake.Status = request.NewStatus;
        intake.ReviewedAt = DateTime.UtcNow;
        intake.ReviewedByDoctorId = doctorId;

        _preVisitIntakeRepository.Update(intake);
        await _unitOfWork.SaveAsync(cancellationToken);

        _logger.LogInformation("Intake {IntakeId} status changed from {OldStatus} to {NewStatus} by doctor {DoctorId}",
            id, oldStatus, request.NewStatus, doctorId);

        var response = _mapper.Map<PreVisitIntakeResponse>(intake);
        return Result.Success(response);
    }

    public async Task<Result<PreVisitIntakeResponse>> ConvertToPatientAsync(Guid id, ConvertIntakeToPatientRequest request, Guid doctorId, CancellationToken cancellationToken = default)
    {
        var intake = await _preVisitIntakeRepository.GetByIdAsync(id, cancellationToken);
        if (intake is null)
            return Result.Failure<PreVisitIntakeResponse>(IntakeErrors.IntakeNotFound);

        if (intake.DoctorId != doctorId)
            return Result.Failure<PreVisitIntakeResponse>(IntakeErrors.UnauthorizedDoctor);

        if (intake.ConvertedToPatientId is not null)
            return Result.Failure<PreVisitIntakeResponse>(IntakeErrors.AlreadyConverted);

        if (intake.Status != IntakeStatus.Approved)
        {
            _logger.LogWarning("Convert-to-patient attempted on intake {IntakeId} with status {Status} (requires Approved) by doctor {DoctorId}",
                id, intake.Status, doctorId);
            return Result.Failure<PreVisitIntakeResponse>(IntakeErrors.InvalidStatusTransition);
        }

        // ADDED: pull patient fields out of the dynamic form submission instead of
        // reading intake.PatientName/PatientEmail/PatientPhone (removed from the entity).
        var submission = DeserializeSubmissionJson(intake.FormSubmissionData);
        if (submission is null)
            return Result.Failure<PreVisitIntakeResponse>(IntakeErrors.InvalidSubmission); // TODO: verify this error constant exists

        var fullName = ExtractInputValuesHelper.ExtractAnswerString(submission, "question_default_full_name", "text");
        var email = ExtractInputValuesHelper.ExtractAnswerString(submission, "question_default_email", "email");
        var phone = ExtractInputValuesHelper.ExtractAnswerString(submission, "question_default_phone", "phone");
        var gender = ExtractInputValuesHelper.ExtractAnswerString(submission, "question_default_gender", "radio");
        var dateOfBirth = ExtractInputValuesHelper.ExtractAnswerDate(submission, "question_default_dob", "date");

        if (string.IsNullOrWhiteSpace(fullName))
            return Result.Failure<PreVisitIntakeResponse>(IntakeErrors.InvalidSubmission); // TODO: verify this error constant exists — Patient.FullName has no default

        var token = $"patient-qr-{Guid.NewGuid():N}";

        var resolvedEmail = string.IsNullOrWhiteSpace(email)
            ? $"converted-{Guid.NewGuid():N}@physioassist.local"
            : email;

        var patient = new Patient
        {
            FullName = fullName,
            EmailAddress = resolvedEmail,
            PhoneNumber = phone ?? string.Empty,
            Gender = gender ?? string.Empty,
            DateOfBirth = dateOfBirth, // null when not present in the submission — fine, now nullable
            QRCodeToken = token,
            Status = PatientStatus.Active
        };

        await _patientRepo.AddAsync(patient);
        await _unitOfWork.SaveAsync(cancellationToken);

        var doctorPatient = new DoctorPatient
        {
            DoctorId = doctorId,
            PatientId = patient.Id,
            IsPrimary = true,
            AssignedAt = DateTime.UtcNow,
            AccessLevel = AccessLevel.FullAccess,
            Status = DoctorPatientStatus.Active
        };

        await _doctorPatientRepo.AddAsync(doctorPatient);
        await _unitOfWork.SaveAsync(cancellationToken);

        intake.ConvertedToPatientId = patient.Id;
        intake.Status = IntakeStatus.Converted;
        intake.ReviewedAt = DateTime.UtcNow;
        intake.ReviewedByDoctorId = doctorId;

        _preVisitIntakeRepository.Update(intake);
        await _unitOfWork.SaveAsync(cancellationToken);

        _logger.LogInformation("Intake {IntakeId} converted to patient {PatientId} by doctor {DoctorId}",
            id, patient.Id, doctorId);

        var response = _mapper.Map<PreVisitIntakeResponse>(intake);
        return Result.Success(response);
    }

    private DynamicFormSubmissionDto? DeserializeSubmissionJson(string submissionJson)
    {
        try
        {
            return JsonSerializer.Deserialize<DynamicFormSubmissionDto>(submissionJson, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private DynamicFormSchemaDto? DeserializeSchemaJson(string schemaJson)
    {
        try
        {
            return JsonSerializer.Deserialize<DynamicFormSchemaDto>(schemaJson, _jsonOptions);
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
