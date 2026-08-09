using FuzzySharp;
using PhysioAssist.Api.Modules.PatientModule.Entities;
using PhysioAssist.Api.Modules.PatientModule.Errors;
using PhysioAssist.Api.Modules.PatientModule.Repositories;
using PhysioAssist.Api.Shared.Dtos.Patient;
using PhysioAssist.Api.Shared.Interfaces.Scheduling;

namespace PhysioAssist.Api.Modules.PatientModule.Services;

public class PatientQueryService(
    ApplicationDbContext dbContext,
    IUnitOfWork _unitOfWork, IPatientRepo _patientRepo,
    IDoctorPatientRepo _doctorPatientRepo,
    IPatientTimePreferenceParser _preferenceParser,
    IHttpContextAccessor httpContextAccessor,
    ILogger<PatientQueryService> _logger) : IPatientQueryService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private Guid DoctorId
    {
        get => Guid.Parse(httpContextAccessor.HttpContext?.User.GetUserId() ?? throw new InvalidOperationException("User ID not found in context."));

    }

    public async Task<List<PatientLookupResult>> FindByNameAsync(string namePart, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(namePart))
            return [];

        var doctorId = DoctorId;

        // Pull the doctor's active patients only — bounded set, safe to score in-memory
        var candidates = await _dbContext.Set<Patient>()
            .Where(p => p.DoctorPatients.Any(dp =>
                dp.DoctorId == doctorId &&
                dp.Status == DoctorPatientStatus.Active))
            .Select(p => new PatientLookupResult(p.Id, p.FullName, p.PatientCaseNotes))
            .ToListAsync(ct);

        const int minScore = 70; // tune this: lower = more forgiving, more false positives

        return candidates
            .Select(p => new
            {
                Patient = p,
                Score = Fuzz.WeightedRatio(namePart, p.FullName)
            })
            .Where(x => x.Score >= minScore)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Patient)
            .ToList();
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
        var rawEmail = request.Email?.Trim();
        Patient? existingPatient = null;

        if (!string.IsNullOrWhiteSpace(rawEmail))
        {
            existingPatient = await _patientRepo.GetByEmailAsync(rawEmail);
        }

        if (existingPatient != null)
        {
            // Patient already exists with this email — link to doctor if not already linked
            var existingDoctorPatient = await _dbContext.Set<DoctorPatient>()
                .FirstOrDefaultAsync(dp => dp.DoctorId == request.DoctorId && dp.PatientId == existingPatient.Id, cancellationToken);

            if (existingDoctorPatient == null)
            {
                var newDoctorPatient = new DoctorPatient
                {
                    DoctorId = request.DoctorId,
                    PatientId = existingPatient.Id,
                    IsPrimary = true,
                    AssignedAt = DateTime.UtcNow,
                    AccessLevel = AccessLevel.FullAccess,
                    Category = request.PatientCategory,
                    Status = DoctorPatientStatus.Active
                };

                await _doctorPatientRepo.AddAsync(newDoctorPatient);
                await _unitOfWork.SaveAsync(cancellationToken);
            }

            return Result.Success(existingPatient.Id);
        }

        var resolvedEmail = string.IsNullOrWhiteSpace(rawEmail)
            ? $"converted-{Guid.NewGuid():N}@physioassist.local"
            : rawEmail;

        if (resolvedEmail.Length > 200)
            resolvedEmail = resolvedEmail[..200];

        var fullName = string.IsNullOrWhiteSpace(request.FullName) ? "Converted Patient" : request.FullName.Trim();
        if (fullName.Length > 100) fullName = fullName[..100];

        var phone = request.Phone?.Trim() ?? string.Empty;
        if (phone.Length > 20) phone = phone[..20];

        var gender = request.Gender?.Trim() ?? string.Empty;
        if (gender.Length > 10) gender = gender[..10];

        var duplicateByResolvedEmail = await _patientRepo.GetByEmailAsync(resolvedEmail);
        if (duplicateByResolvedEmail is not null)
            return Result.Failure<Guid>(PatientErrors.DuplicateEmail);

        var patient = new Patient
        {
            FullName = fullName,
            EmailAddress = resolvedEmail,
            PhoneNumber = phone,
            Gender = gender,
            DateOfBirth = request.DateOfBirth,
            QRCodeToken = $"patient-qr-{Guid.NewGuid():N}",
            Status = PatientStatus.Active,
            PatientFreeTime = request.FreeTime ?? string.Empty,
            PatientCaseNotes = request.Notes ?? string.Empty
        };

        if (!string.IsNullOrWhiteSpace(request.FreeTime))
        {
            try
            {
                var preferenceResult = await _preferenceParser.ParseAsync(request.FreeTime, cancellationToken);

                if (preferenceResult.IsSuccess)
                {
                    var parsed = preferenceResult.Value;
                    patient.ParsedPreferredDayToken = parsed.DayToken;
                    patient.ParsedPreferredExplicitDate = parsed.ExplicitDate;

                    foreach (var g in parsed.Groups)
                    {
                        patient.PreferredTimeSlots.Add(new PatientPreferredTimeSlot
                        {
                            Weekdays = g.Weekdays,
                            TimeFrom = g.TimeFrom,
                            TimeTo = g.TimeTo
                        });
                    }
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

        var patient = await _patientRepo.GetByPatientWithFreeTimeSlotsAsync(patientId, cancellationToken);

        if (patient is null)
            return Result.Failure<PatientTimePreferenceInfo>(PatientErrors.NotFound);

        var parsed = new PatientTimePreferenceDto();
        try
        {
            var preferenceResult = await _preferenceParser.ParseAsync(freeTimeOverrideText, cancellationToken);

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
            patient.ParsedPreferredExplicitDate = parsed.ExplicitDate;

            if (patient.PreferredTimeSlots.Any())
            {
                _dbContext.RemoveRange(patient.PreferredTimeSlots);
            }

            foreach (var g in parsed.Groups)
            {
                var newSlot = new PatientPreferredTimeSlot { 
                    PatientId = patient.Id,
                    Weekdays = g.Weekdays,
                    TimeFrom = g.TimeFrom,
                    TimeTo = g.TimeTo
                };

              _dbContext.PatientPreferredTimeSlots.Add(newSlot);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Parsed value drives *this* search either way — persisted or not.
        return Result.Success(new PatientTimePreferenceInfo(
            parsed.DayToken,
            parsed.ExplicitDate,
            parsed.Groups));
    }
    public async Task<Dictionary<Guid, PatientLookupResult>> GetPatientsByIdsAsync(
        IEnumerable<Guid> patientIds,
        CancellationToken cancellationToken = default)
    {
        var ids = patientIds.Distinct().ToList();

        return await dbContext.Patients
            .Where(p => ids.Contains(p.Id))
            .Select(p => new PatientLookupResult(p.Id, p.FullName, p.PatientCaseNotes))
            .ToDictionaryAsync(p => p.Id, cancellationToken);
    }
    private async Task<Result<PatientTimePreferenceInfo>> GetPatientTimePreferenceAsync(
    Guid patientId, CancellationToken cancellationToken = default)
    {
        var patient = await _patientRepo.GetByPatientWithFreeTimeSlotsAsync(patientId, cancellationToken);

        if (patient is null)
            return Result.Failure<PatientTimePreferenceInfo>(PatientErrors.NotFound);

        var groups = patient.PreferredTimeSlots
            .Select(s => new PatientPreferredTimeGroupDto
            {
                Weekdays = s.Weekdays,
                TimeFrom = s.TimeFrom,
                TimeTo = s.TimeTo
            })
            .ToList();

        return Result.Success(new PatientTimePreferenceInfo(
            patient.ParsedPreferredDayToken,
            patient.ParsedPreferredExplicitDate,
            groups));
    }
}

