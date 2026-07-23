using Mapster;
using Microsoft.EntityFrameworkCore;
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

        return await _dbContext.Set<Patient>()
            .Where(p => EF.Functions.Like(p.FullName, $"%{namePart}%"))
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

        var occupation = request.Occupation?.Trim() ?? string.Empty;
        if (occupation.Length > 200) occupation = occupation[..200];

        var patient = new Patient
        {
            FullName = fullName,
            EmailAddress = resolvedEmail,
            PhoneNumber = phone,
            Gender = gender,
            DateOfBirth = request.DateOfBirth,
            QRCodeToken = $"patient-qr-{Guid.NewGuid():N}",
            Occupation = occupation,
            Status = PatientStatus.Active
        };

        try
        {
            await _patientRepo.AddAsync(patient);
            await _unitOfWork.SaveAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Fallback for unexpected duplicate email constraint violation
            patient.EmailAddress = $"converted-{Guid.NewGuid():N}@physioassist.local";
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        var doctorPatientLink = new DoctorPatient
        {
            DoctorId = request.DoctorId,
            PatientId = patient.Id,
            IsPrimary = true,
            AssignedAt = DateTime.UtcNow,
            AccessLevel = AccessLevel.FullAccess,
            Category = request.PatientCategory,
            Status = DoctorPatientStatus.Active
        };

        await _doctorPatientRepo.AddAsync(doctorPatientLink);
        await _unitOfWork.SaveAsync(cancellationToken);

        return Result.Success(patient.Id);
    }
}
