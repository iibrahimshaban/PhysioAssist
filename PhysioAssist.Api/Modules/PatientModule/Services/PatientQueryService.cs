using Mapster;
using PhysioAssist.Api.Modules.PatientModule.Entities;
using PhysioAssist.Api.Modules.PatientModule.Errors;
using PhysioAssist.Api.Modules.PatientModule.Repositories;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Dtos.Patient;
using PhysioAssist.Api.Shared.Interfaces.Common;
using PhysioAssist.Api.Shared.Interfaces.Exposed;

namespace PhysioAssist.Api.Modules.PatientModule.Services;

public class PatientQueryService(ApplicationDbContext dbContext, IUnitOfWork _unitOfWork, IPatientRepo _patientRepo,
IDoctorPatientRepo _doctorPatientRepo) : IPatientQueryService
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
            Status = PatientStatus.Active
        };

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

        return Result.Success(patient.Id);
    }
}
