using MapsterMapper;
using PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;
using PhysioAssist.Api.Modules.Intake.DTOs.FormSchemas;
using PhysioAssist.Api.Modules.Intake.DTOs.PublicAccess;
using PhysioAssist.Api.Modules.Intake.DTOs.Submissions;
using PhysioAssist.Api.Modules.Intake.Entities;
using PhysioAssist.Api.Modules.Intake.Errors;
using PhysioAssist.Api.Modules.Intake.Helpers;
using PhysioAssist.Api.Modules.Intake.Repositories;
using PhysioAssist.Api.Shared.Consts;
using PhysioAssist.Api.Shared.Dtos.Patient;
using PhysioAssist.Api.Shared.Interfaces.Common;
using PhysioAssist.Api.Shared.Interfaces.Exposed;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.QR;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IQRService = PhysioAssist.Api.Shared.Interfaces.Common.IQRService;

namespace PhysioAssist.Api.Modules.Intake.Services;

public class IntakeService(
    IPatientFormSchemaRepository patientFormSchemaRepository,
    IPreVisitIntakeRepository preVisitIntakeRepository,
    IDynamicFormValidationService dynamicFormValidationService,
    IQRService qrService,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ILogger<IntakeService> logger,
    IPatientQueryService _patientQueryService,
    ApplicationDbContext context
) : IIntakeService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static string GenerateShortCode(int length = 8)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        using var rng = RandomNumberGenerator.Create();
        byte[] data = new byte[length];
        rng.GetBytes(data);
        StringBuilder result = new(length);
        foreach (byte b in data)
        {
            result.Append(chars[b % chars.Length]);
        }
        return result.ToString();
    }

    private async Task<string> GenerateUniqueFormShortCodeAsync(CancellationToken cancellationToken)
    {
        string? shortCode;
        do
        {
            shortCode = GenerateShortCode();
        } while (await _context.PatientFormSchemas.AnyAsync(s => s.ShortCode == shortCode, cancellationToken));
        return shortCode;
    }

    private async Task<string> GenerateUniqueIntakeShortCodeAsync(CancellationToken cancellationToken)
    {
        string? shortCode;
        do
        {
            shortCode = GenerateShortCode();
        } while (await _context.PreVisitIntakes.AnyAsync(i => i.ShortCode == shortCode, cancellationToken));
        return shortCode;
    }

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
    private readonly ApplicationDbContext _context = context;

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
        schema.ShortCode = await GenerateUniqueFormShortCodeAsync(cancellationToken);

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

        var oldSchemaDto = DeserializeSchemaJson(schema.SchemaJson);
        if (oldSchemaDto is not null)
        {
            var lockedIds = oldSchemaDto.Sections
                .SelectMany(s => s.Groups)
                .SelectMany(g => g.Questions)
                .Where(q => q.IsLocked)
                .Select(q => q.QuestionId)
                .ToHashSet();

            if (lockedIds.Count > 0)
            {
                var newIds = schemaDto.Sections
                    .SelectMany(s => s.Groups)
                    .SelectMany(g => g.Questions)
                    .Select(q => q.QuestionId)
                    .ToHashSet();

                var missingLocked = lockedIds.Where(id => !newIds.Contains(id)).ToList();
                if (missingLocked.Count > 0)
                {
                    return Result.Failure<FormSchemaResponse>(IntakeErrors.LockedQuestionRemoved);
                }
            }
        }

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

        if (schemas.Count == 0)
        {
            await SeedDefaultSchemaAsync(doctorId, cancellationToken);
            schemas = await _patientFormSchemaRepository.GetByDoctorAsync(doctorId, cancellationToken);
        }

        var responses = _mapper.Map<List<FormSchemaSummaryResponse>>(schemas);

        var schemaIds = schemas.Select(s => s.Id).ToList();
        var submissionCounts = await context.PreVisitIntakes
            .Where(i => schemaIds.Contains(i.FormSchemaId))
            .GroupBy(i => i.FormSchemaId)
            .Select(g => new { SchemaId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.SchemaId, g => g.Count, cancellationToken);

        foreach (var response in responses)
        {
            response.SubmissionCount = submissionCounts.GetValueOrDefault(response.Id, 0);
            var schema = schemas.FirstOrDefault(s => s.Id == response.Id);
            if (schema is not null)
            {
                response.FieldsCount = CountFieldsInSchemaJson(schema.SchemaJson);
            }
        }

        return Result.Success<IReadOnlyList<FormSchemaSummaryResponse>>(responses);
    }

    private async Task SeedDefaultSchemaAsync(Guid doctorId, CancellationToken cancellationToken)
    {
        var defaultDto = DefaultIntakeSchemaTemplate.Build();
        var defaultJson = JsonSerializer.Serialize(defaultDto, _jsonOptions);

        var schema = new PatientFormSchema
        {
            Name = "Default Intake Form",
            Description = "Welcome to our clinic, please fill out the form",
            SchemaJson = defaultJson,
            DoctorId = doctorId,
            Version = 1,
            Status = FormSchemaStatus.Published,
            IsDefault = true,
            PublishedAt = DateTime.UtcNow,
        };

        await _patientFormSchemaRepository.AddAsync(schema, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
    }

    private static int CountFieldsInSchemaJson(string schemaJson)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<DynamicFormSchemaDto>(schemaJson, _jsonOptions);
            return dto?.Sections.Sum(s => s.Groups.Sum(g => g.Questions.Count)) ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    public async Task<Result<FormSchemaResponse>> GetDefaultFormSchemaAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        var schema = await _patientFormSchemaRepository.GetDefaultForDoctorAsync(doctorId, cancellationToken);
        if (schema is null)
            return Result.Failure<FormSchemaResponse>(IntakeErrors.SchemaNotFound);

        var response = _mapper.Map<FormSchemaResponse>(schema);
        return Result.Success(response);
    }

    public async Task<Result<FormSchemaResponse>> GenerateDefaultFormSchemaAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        var schemaDto = DefaultIntakeSchemaTemplate.Build();
        var schemaJson = JsonSerializer.Serialize(schemaDto, _jsonOptions);

        var createRequest = new CreateFormSchemaRequest
        {
            Name = "Default Intake Form",
            Description = "Welcome to our clinic, please fill out the form",
            SchemaJson = schemaJson,
            IsDefault = true,
            ShowPainMap = true,
        };

        var createResult = await CreateFormSchemaAsync(createRequest, doctorId, cancellationToken);
        if (createResult.IsFailure)
            return createResult;

        var publishRequest = new PublishFormSchemaRequest { Version = createResult.Value.Version };
        var publishResult = await PublishFormSchemaAsync(createResult.Value.Id, publishRequest, doctorId, cancellationToken);

        return publishResult.IsFailure ? publishResult : createResult;
    }

    private const int MaximumCopiesPerForm = 10;

    public async Task<Result<FormSchemaResponse>> DuplicateFormSchemaAsync(Guid schemaId, Guid doctorId, CancellationToken cancellationToken = default)
    {
        var originalSchema = await _patientFormSchemaRepository.GetByIdAsync(schemaId, cancellationToken);
        if (originalSchema is null)
            return Result.Failure<FormSchemaResponse>(IntakeErrors.SchemaNotFound);

        if (originalSchema.DoctorId != doctorId)
            return Result.Failure<FormSchemaResponse>(IntakeErrors.UnauthorizedDoctor);

        // Check copy limit
        var existingCopies = await _patientFormSchemaRepository.GetCopiesByOriginalFormIdAsync(
            originalSchema.OriginalFormId ?? originalSchema.Id, doctorId, cancellationToken);
        if (existingCopies.Count >= MaximumCopiesPerForm)
            return Result.Failure<FormSchemaResponse>(IntakeErrors.CopyLimitExceeded);

        // Determine original name and form ID
        var rootOriginalId = originalSchema.OriginalFormId ?? originalSchema.Id;
        var originalName = originalSchema.OriginalName ?? originalSchema.Name;

        // Generate next copy number
        var nextCopyNumber = existingCopies.Any()
            ? existingCopies.Max(c => c.CopyNumber ?? 0) + 1
            : 1;

        // Generate smart name
        string newName;
        int attempt = 0;
        do
        {
            newName = $"{originalName} (Copy #{nextCopyNumber + attempt})";
            attempt++;
        } while (await _patientFormSchemaRepository.ExistsNameForDoctorAsync(doctorId, newName, null, cancellationToken));

        // Create new schema
        var newSchema = new PatientFormSchema
        {
            Name = newName,
            ShortCode = await GenerateUniqueFormShortCodeAsync(cancellationToken),
            Description = originalSchema.Description,
            SchemaJson = originalSchema.SchemaJson,
            DoctorId = doctorId,
            Version = 1,
            Status = FormSchemaStatus.Draft,
            IsDefault = false,
            ShowPainMap = originalSchema.ShowPainMap,
            SchemaHash = originalSchema.SchemaHash,
            OriginalFormId = rootOriginalId,
            CopyNumber = nextCopyNumber,
            OriginalName = originalName,
            CreatedById = DefaultUsers.UserId,
            CreatedAt = DateTime.UtcNow
        };

        await _patientFormSchemaRepository.AddAsync(newSchema, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        var response = _mapper.Map<FormSchemaResponse>(newSchema);
        return Result.Success(response);
    }

    public async Task<Result> DeleteFormSchemaAsync(Guid schemaId, Guid doctorId, CancellationToken cancellationToken = default)
    {
        var schema = await _patientFormSchemaRepository.GetByIdAsync(schemaId, cancellationToken);
        if (schema is null)
            return Result.Failure(IntakeErrors.SchemaNotFound);

        if (schema.DoctorId != doctorId)
            return Result.Failure(IntakeErrors.UnauthorizedDoctor);

        if (schema.IsDefault)
            return Result.Failure(IntakeErrors.CannotDeleteDefaultSchema);

        _patientFormSchemaRepository.Remove(schema);
        await _unitOfWork.SaveAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ArchiveFormSchemaAsync(Guid schemaId, Guid doctorId, CancellationToken cancellationToken = default)
    {
        var schema = await _patientFormSchemaRepository.GetByIdAsync(schemaId, cancellationToken);
        if (schema is null)
            return Result.Failure(IntakeErrors.SchemaNotFound);

        if (schema.DoctorId != doctorId)
            return Result.Failure(IntakeErrors.UnauthorizedDoctor);

        if (schema.Status == FormSchemaStatus.Archived)
            return Result.Failure(IntakeErrors.SchemaAlreadyArchived);

        if (schema.Status != FormSchemaStatus.Published)
            return Result.Failure(IntakeErrors.SchemaNotPublished);

        schema.Status = FormSchemaStatus.Archived;
        _patientFormSchemaRepository.Update(schema);
        await _unitOfWork.SaveAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UnarchiveFormSchemaAsync(Guid schemaId, Guid doctorId, CancellationToken cancellationToken = default)
    {
        var schema = await _patientFormSchemaRepository.GetByIdAsync(schemaId, cancellationToken);
        if (schema is null)
            return Result.Failure(IntakeErrors.SchemaNotFound);

        if (schema.DoctorId != doctorId)
            return Result.Failure(IntakeErrors.UnauthorizedDoctor);

        if (schema.Status != FormSchemaStatus.Archived)
            return Result.Failure(IntakeErrors.SchemaNotArchived);

        schema.Status = FormSchemaStatus.Published;
        _patientFormSchemaRepository.Update(schema);
        await _unitOfWork.SaveAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<GenerateIntakeQrLinkResponse>> GenerateIntakeQrLinkAsync(Guid schemaId, GenerateIntakeQrLinkRequest request, Guid doctorId, CancellationToken cancellationToken = default)
    {
        var schema = await _patientFormSchemaRepository.GetByIdAsync(schemaId, cancellationToken);
        if (schema is null)
            return Result.Failure<GenerateIntakeQrLinkResponse>(IntakeErrors.SchemaNotFound);

        if (schema.DoctorId != doctorId)
            return Result.Failure<GenerateIntakeQrLinkResponse>(IntakeErrors.UnauthorizedDoctor);

        if (schema.Status != FormSchemaStatus.Published)
            return Result.Failure<GenerateIntakeQrLinkResponse>(IntakeErrors.SchemaNotPublished);

        var expiry = DateTime.UtcNow.AddMonths(request.ExpiryMonths);
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

        if (schema.DoctorId is Guid doctorId)
        {
            var doctor = await context.Doctors.FindAsync([doctorId], cancellationToken);
            if (doctor is not null)
                response = response with { ClinicName = doctor.ClinicName ?? string.Empty };
        }

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

        var submissionDto = ExtractInputValuesHelper.DeserializeSubmissionJson(request.FormSubmissionData);
        if (submissionDto is null)
            return Result.Failure<PublicIntakeSubmissionResponse>(IntakeErrors.InvalidSubmission);

        var validationResult = _dynamicFormValidationService.ValidateSubmissionAgainstSchema(schemaDto, submissionDto);
        if (validationResult.IsFailure)
            return Result.Failure<PublicIntakeSubmissionResponse>(validationResult.Error);

        var intake = _mapper.Map<PreVisitIntake>(request);
        intake.ShortCode = await GenerateUniqueIntakeShortCodeAsync(cancellationToken);
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

        foreach (var intake in intakes)
        {
            _logger.LogInformation("IntakeService.GetSubmissionsAsync: intake.Id={IntakeId}, formSubmissionData={FormSubmissionData}", 
                intake.Id, intake.FormSubmissionData);
            var patientName = ExtractInputValuesHelper.ExtractPatientNameSafe(intake.FormSubmissionData);
            _logger.LogInformation("IntakeService.GetSubmissionsAsync: intake.Id={IntakeId}, extracted patientName={PatientName}", 
                intake.Id, patientName);
        }

        var responses = intakes.Select(MapToPreVisitIntakeResponse).ToList();

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
            return Result.Success(MapToPreVisitIntakeResponse(intake));

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

        return Result.Success(MapToPreVisitIntakeResponse(intake));
    }

    public async Task<Result<PreVisitIntakeResponse>> ConvertToPatientAsync(
    Guid id, ConvertIntakeToPatientRequest request, Guid doctorId, CancellationToken cancellationToken = default)
    {
        var intake = await _preVisitIntakeRepository.GetByIdAsync(id, cancellationToken);
        if (intake is null)
            return Result.Failure<PreVisitIntakeResponse>(IntakeErrors.IntakeNotFound);

        if (intake.DoctorId != doctorId)
            return Result.Failure<PreVisitIntakeResponse>(IntakeErrors.UnauthorizedDoctor);

        if (intake.ConvertedToPatientId is not null)
            return Result.Failure<PreVisitIntakeResponse>(IntakeErrors.AlreadyConverted);

        if (!string.IsNullOrWhiteSpace(request.FormSubmissionData))
            intake.FormSubmissionData = request.FormSubmissionData;

        if (request.PainPointsData is not null)
            intake.PainPointsData = string.IsNullOrWhiteSpace(request.PainPointsData) ? null : request.PainPointsData;

        var submission = ExtractInputValuesHelper.DeserializeSubmissionJson(intake.FormSubmissionData);
        if (submission is null)
            return Result.Failure<PreVisitIntakeResponse>(IntakeErrors.InvalidSubmission);

        var schema = await LoadFormSchemaAsync(intake.FormSchemaId, cancellationToken);

        var fullNameQId = ExtractInputValuesHelper.FindQuestionIdByText(schema, "Full Name") 
            ?? ExtractInputValuesHelper.FindQuestionIdByText(schema, "Name")
            ?? "question_default_full_name";
        var emailQId = ExtractInputValuesHelper.FindQuestionIdByText(schema, "Email") 
            ?? ExtractInputValuesHelper.FindQuestionIdByText(schema, "E-mail")
            ?? "question_default_email";
        var phoneQId = ExtractInputValuesHelper.FindQuestionIdByText(schema, "Phone") 
            ?? ExtractInputValuesHelper.FindQuestionIdByText(schema, "Phone Number")
            ?? "question_default_phone";
        var genderQId = ExtractInputValuesHelper.FindQuestionIdByText(schema, "Gender") 
            ?? "question_default_gender";
        var dobQId = ExtractInputValuesHelper.FindQuestionIdByText(schema, "Date of Birth") 
            ?? ExtractInputValuesHelper.FindQuestionIdByText(schema, "DOB")
            ?? "question_default_dob";
        var jobQId = ExtractInputValuesHelper.FindQuestionIdByText(schema, "Occupation") 
            ?? ExtractInputValuesHelper.FindQuestionIdByText(schema, "Job")
            ?? "question_default_job";

        var fullName = ExtractInputValuesHelper.ExtractAnswerString(submission, fullNameQId, ExtractInputValuesHelper.GetWrapperKey(schema, fullNameQId));
        var email = ExtractInputValuesHelper.ExtractAnswerString(submission, emailQId, ExtractInputValuesHelper.GetWrapperKey(schema, emailQId));
        var phone = ExtractInputValuesHelper.ExtractAnswerString(submission, phoneQId, ExtractInputValuesHelper.GetWrapperKey(schema, phoneQId));
        var gender = ExtractInputValuesHelper.ExtractAnswerString(submission, genderQId, ExtractInputValuesHelper.GetWrapperKey(schema, genderQId));
        DateTime? dateOfBirth = ExtractInputValuesHelper.ExtractAnswerDate(submission, dobQId, ExtractInputValuesHelper.GetWrapperKey(schema, dobQId));
        var job = ExtractInputValuesHelper.ExtractAnswerString(submission, jobQId, ExtractInputValuesHelper.GetWrapperKey(schema, jobQId));

        // If full name is empty, generate a placeholder
        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = $"Converted Patient {Guid.NewGuid():N}";
        }

        var createPatientResult = await _patientQueryService.CreatePatientFromIntakeAsync(
            new CreatePatientFromIntakeRequest(
                fullName,
                email,
                phone,
                gender,
                dateOfBirth,
                job,
                doctorId,
                ExtractInputValuesHelper.ExtractPatientCategory(intake.PainPointsData)),
            cancellationToken);

        if (createPatientResult.IsFailure)
            return Result.Failure<PreVisitIntakeResponse>(createPatientResult.Error);

        intake.ConvertedToPatientId = createPatientResult.Value;
        intake.Status = IntakeStatus.Converted;
        intake.ReviewedAt = DateTime.UtcNow;
        intake.ReviewedByDoctorId = doctorId;

        _preVisitIntakeRepository.Update(intake);
        await _unitOfWork.SaveAsync(cancellationToken);

        _logger.LogInformation("Intake {IntakeId} converted to patient {PatientId} by doctor {DoctorId}",
            id, intake.ConvertedToPatientId, doctorId);

        return Result.Success(MapToPreVisitIntakeResponse(intake));
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



    private async Task<DynamicFormSchemaDto?> LoadFormSchemaAsync(Guid formSchemaId, CancellationToken cancellationToken)
    {
        var schemaEntity = await _patientFormSchemaRepository.GetByIdAsync(formSchemaId, cancellationToken);
        if (schemaEntity is null) return null;
        return DeserializeSchemaJson(schemaEntity.SchemaJson);
    }
    
    private PreVisitIntakeResponse MapToPreVisitIntakeResponse(PreVisitIntake intake)
    {
        var response = _mapper.Map<PreVisitIntakeResponse>(intake);
        return response with
        {
            PatientName = ExtractInputValuesHelper.ExtractPatientNameSafe(intake.FormSubmissionData),
            PainRegionCount = ExtractInputValuesHelper.CountPainRegions(intake.PainPointsData)
        };
    }
}
