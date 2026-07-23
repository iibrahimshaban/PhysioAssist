using PhysioAssist.Api.Modules.PatientModule.Entities;
using PhysioAssist.Api.Modules.PatientModule.Errors;
using PhysioAssist.Api.Modules.PatientModule.Repositories;
using PhysioAssist.Api.Shared.Dtos.Patient;
using PhysioAssist.Api.Shared.Interfaces.Ingestion;
using PhysioAssist.Api.Shared.Interfaces.Scheduling;

namespace PhysioAssist.Api.Modules.PatientModule.Services;

public class PatientQueryService(
    ApplicationDbContext dbContext,
    IUnitOfWork _unitOfWork, IPatientRepo _patientRepo,
    IDoctorPatientRepo _doctorPatientRepo,
    IQueryTranslationService _translationService,
    IPatientTimePreferenceParser _preferenceParser,
    ILogger<PatientQueryService> _logger) : IPatientQueryService
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<List<PatientLookupResult>> FindByNameAsync(string namePart, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(namePart))
            return [];

        return await _dbContext.Set<Patient>() // adjust to your actual Patient entity name/namespace
            .Where(p => EF.Functions.Like(p.FullName, $"%{namePart}%")) // adjust FullName to your actual property
            .Select(p => new PatientLookupResult(p.Id, p.FullName))
            .ToListAsync(ct);
    }
    public async Task<PatientCategory?> GetPatientCategoryAsync(Guid doctorId, Guid patientId, CancellationToken ct = default)
    {
        return await _dbContext.Set<DoctorPatient>()
            .Where(dp => dp.DoctorId == doctorId && dp.PatientId == patientId)
            .Select(dp => (PatientCategory?)dp.Category)
            .FirstOrDefaultAsync(ct);
    }
    public async Task<Result<PatientResponse>> GetPatientAsync(Guid patientId, CancellationToken ct = default)
    {
        var patient = await _dbContext.Patients.FindAsync(patientId, ct);

        if (patient == null)
        {
            return Result.Failure<PatientResponse>(PatientErrors.NotFound);
        }

        var response = patient.Adapt<PatientResponse>();

        return Result.Success(response);

    }

    public async Task<Result<List<PatientResponse>>> GetAllPatientsForDoctorAsync(Guid doctorId,CancellationToken ct = default)
    {
        var patientIds = await _dbContext.DoctorPatients
            .Where(dp => dp.DoctorId == doctorId)
            .Select(dp => dp.PatientId)
            .ToListAsync(ct);

        var patients = await _dbContext.Patients
            .Where(p => patientIds.Contains(p.Id))
            .ToListAsync(ct);

        if (!patients.Any())
        {
            return Result.Failure<List<PatientResponse>>(PatientErrors.NotFound);
        }

        var response = patients.Adapt<List<PatientResponse>>();

        return Result.Success(response);
    }


    public async Task<Result<Guid>> CreatePatientFromIntakeAsync(CreatePatientFromIntakeRequest request,
    CancellationToken cancellationToken = default)
    {
        var resolvedEmail = string.IsNullOrWhiteSpace(request.Email)
            ? $"converted-{Guid.NewGuid():N}@physioassist.local"
            : request.Email;

        var patient = new Patient
        {
            FullName = request.FullName,
            EmailAddress = resolvedEmail,
            PhoneNumber = request.Phone ?? string.Empty,
            Gender = request.Gender ?? string.Empty,
            DateOfBirth = request.DateOfBirth,
            QRCodeToken = $"patient-qr-{Guid.NewGuid():N}",
            Occupation = request.Occupation ?? string.Empty,
            Status = PatientStatus.Active,
            PatientFreeTime = request.FreeTime ?? string.Empty
        };

        if (!string.IsNullOrWhiteSpace(request.FreeTime))
        {
            try
            {
                var englishFreeTime = await _translationService.TranslateToEnglishAsync(request.FreeTime, cancellationToken);
                var preferenceResult = await _preferenceParser.ParseAsync(englishFreeTime, cancellationToken);

                if (preferenceResult.IsSuccess)
                {
                    patient.ParsedPreferredDayToken = preferenceResult.Value.DayToken;
                    patient.ParsedPreferredTimeFrom = preferenceResult.Value.PreferredTimeFrom;
                    patient.ParsedPreferredTimeTo = preferenceResult.Value.PreferredTimeTo;
                    patient.ParsedPreferredWeekdays = preferenceResult.Value.PreferredWeekdays;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex,
                    "Free-time translation/parsing unavailable during intake conversion for {FreeTime}; proceeding without parsed preference.",
                    request.FreeTime);
            }
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _patientRepo.AddAsync(patient);
            await _unitOfWork.SaveAsync(cancellationToken);

            var doctorPatient = new DoctorPatient
            {
                DoctorId = request.DoctorId,
                PatientId = patient.Id,
                IsPrimary = true,
                AssignedAt = DateTime.UtcNow,
                AccessLevel = AccessLevel.FullAccess,
                Category = request.PatientCategory,
                Status = DoctorPatientStatus.Active
            };

            await _doctorPatientRepo.AddAsync(doctorPatient);
            await _unitOfWork.SaveAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return Result.Success(patient.Id);
    }

    public async Task<Result<PatientTimePreferenceInfo>> GetPatientTimePreferenceAsync(
    Guid patientId, CancellationToken cancellationToken = default)
    {
        var patient = await _patientRepo.GetByIdAsync(patientId);

        if (patient is null)
            return Result.Failure<PatientTimePreferenceInfo>(PatientErrors.NotFound);

        return Result.Success(new PatientTimePreferenceInfo(
            patient.ParsedPreferredDayToken,
            patient.ParsedPreferredWeekdays,
            patient.ParsedPreferredTimeFrom,
            patient.ParsedPreferredTimeTo)
            );
    }
    public async Task<Result<PatientTimePreferenceInfo>> ResolvePatientTimePreferenceAsync(
        Guid patientId,
        string? freeTimeOverrideText,
        bool persistOverride,
        CancellationToken cancellationToken = default)
    {
        // No override typed this session — behave exactly as before, read whatever's
        // already persisted on the patient.
        if (string.IsNullOrWhiteSpace(freeTimeOverrideText))
            return await GetPatientTimePreferenceAsync(patientId, cancellationToken);

        var patient = await _patientRepo.GetByIdAsync(patientId);
        if (patient is null)
            return Result.Failure<PatientTimePreferenceInfo>(PatientErrors.NotFound);

        var parsed = new PatientTimePreferenceDto();
        try
        {
            var englishFreeTime = await _translationService.TranslateToEnglishAsync(freeTimeOverrideText, cancellationToken);
            var preferenceResult = await _preferenceParser.ParseAsync(englishFreeTime, cancellationToken);

            if (preferenceResult.IsSuccess)
                parsed = preferenceResult.Value;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex,
                "Free-time translation/parsing unavailable while resolving override for patient {PatientId}; proceeding without parsed preference.",
                patientId);
        }

        if (persistOverride)
        {
            patient.PatientFreeTime = freeTimeOverrideText;
            patient.ParsedPreferredDayToken = parsed.DayToken;
            patient.ParsedPreferredTimeFrom = parsed.PreferredTimeFrom;
            patient.ParsedPreferredTimeTo = parsed.PreferredTimeTo;
            patient.ParsedPreferredWeekdays = parsed.PreferredWeekdays;

            await _unitOfWork.SaveAsync(cancellationToken);
        }

        // Parsed value drives *this* search either way — persisted or not.
        return Result.Success(new PatientTimePreferenceInfo(
            parsed.DayToken,
            parsed.PreferredWeekdays,
            parsed.PreferredTimeFrom,
            parsed.PreferredTimeTo));
    }
}
