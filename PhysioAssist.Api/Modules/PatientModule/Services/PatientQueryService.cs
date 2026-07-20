using Mapster;
using PhysioAssist.Api.Modules.PatientModule.Entities;
using PhysioAssist.Api.Modules.PatientModule.Errors;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Dtos.Patient;

namespace PhysioAssist.Api.Modules.PatientModule.Services;

public class PatientQueryService : IPatientQueryService
{
    private readonly ApplicationDbContext _dbContext;

    public PatientQueryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

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

   
}
