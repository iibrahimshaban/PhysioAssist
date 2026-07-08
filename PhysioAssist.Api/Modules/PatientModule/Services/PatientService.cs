using Mapster;
using MapsterMapper;
using PhysioAssist.Api.Modules.PatientModule.DTOs;
using PhysioAssist.Api.Modules.PatientModule.Entities;
using PhysioAssist.Api.Modules.PatientModule.Errors;
using PhysioAssist.Api.Modules.PatientModule.Repositories;
using PhysioAssist.Api.Shared.Interfaces;

namespace PhysioAssist.Api.Modules.PatientModule.Services
{
    public class PatientService : IPatientService
    {
        private readonly IPatientRepo _patientRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IDoctorPatientRepo _doctorPatientRepo;

        public PatientService(IPatientRepo patientRepo, IUnitOfWork unitOfWork, IMapper mapper, IDoctorPatientRepo doctorPatientRepo)
        {
            _patientRepo = patientRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _doctorPatientRepo = doctorPatientRepo;
        }

        public async Task<Result<PatientResponse>> CreateAsync(PatientRequest request)
        {
            var existingPatient = await _patientRepo.GetByPhoneAsync(request.PhoneNumber);
            if (existingPatient is not null)
                return Result.Failure<PatientResponse>(PatientErrors.DuplicatePhone);

            var patient = request.Adapt<Patient>();
            await _patientRepo.AddAsync(patient);
            await _unitOfWork.SaveAsync(CancellationToken.None);

            return Result.Success(patient.Adapt<PatientResponse>());
        }

        public async Task<Result<IEnumerable<PatientResponse>>> GetAllAsync()
        {
            var result = await _patientRepo.GetAllAsync();


            return Result.Success(result.Adapt<IEnumerable<PatientResponse>>());
        }

        public async Task<Result<PatientResponse>> GetByIdAsync(Guid patientId)
        {
            var patient = await _patientRepo.GetByIdAsync(patientId);
            if (patient is null)
                return Result.Failure<PatientResponse>(PatientErrors.NotFound);

            return Result.Success(patient.Adapt<PatientResponse>());
        }

        public async Task<Result<PatientResponse>> UpdateAsync(Guid patientId, PatientRequest request)
        {
            var patient = await _patientRepo.GetByIdAsync(patientId);
            if (patient is null)
                return Result.Failure<PatientResponse>(PatientErrors.NotFound);

            request.Adapt(patient);
            _patientRepo.Update(patient);
            await _unitOfWork.SaveAsync(CancellationToken.None);

            return Result.Success(patient.Adapt<PatientResponse>());
        }

        public async Task<Result> DeleteAsync(Guid patientId)
        {
            var patient = await _patientRepo.GetByIdAsync(patientId);
            if (patient is null)
                return Result.Failure(PatientErrors.NotFound);

            _patientRepo.Delete(patient);
            await _unitOfWork.SaveAsync(CancellationToken.None);

            return Result.Success();
        }

        public async Task<Result> UpdateStatusAsync(Guid patientId, PatientStatus status)
        {
            var patient = await _patientRepo.GetByIdAsync(patientId);
            if (patient is null)
                return Result.Failure(PatientErrors.NotFound);

            patient.Status = status;
            _patientRepo.Update(patient);
            await _unitOfWork.SaveAsync(CancellationToken.None);

            return Result.Success();
        }

        // pat doc

        public async Task<Result> AssignPatientAsync(Guid doctorId, Guid patientId)
        {
            var existing = await _doctorPatientRepo.GetByDoctorAndPatientAsync(doctorId, patientId);
            if (existing is not null)
                return Result.Failure(PatientErrors.AlreadyAssigned);

            var doctorPatient = new DoctorPatient
            {
                DoctorId = doctorId,
                PatientId = patientId,
                AssignedAt = DateTime.UtcNow,
                Status = DoctorPatientStatus.Active
            };

            await _doctorPatientRepo.AddAsync(doctorPatient);
            await _unitOfWork.SaveAsync(CancellationToken.None);

            return Result.Success();
        }

        public async Task<Result> DischargePatientAsync(Guid doctorId, Guid patientId)
        {
            var doctorPatient = await _doctorPatientRepo.GetByDoctorAndPatientAsync(doctorId, patientId);
            if (doctorPatient is null)
                return Result.Failure(PatientErrors.NotFound);

            doctorPatient.Status = DoctorPatientStatus.Revoked;
            _doctorPatientRepo.Update(doctorPatient);
            await _unitOfWork.SaveAsync(CancellationToken.None);

            return Result.Success();
        }

        public async Task<Result> SetPrimaryDoctorAsync(Guid doctorId, Guid patientId)
        {
            var doctorPatient = await _doctorPatientRepo.GetByDoctorAndPatientAsync(doctorId, patientId);
            if (doctorPatient is null)
                return Result.Failure(PatientErrors.NotFound);

            doctorPatient.IsPrimary = true;
            _doctorPatientRepo.Update(doctorPatient);
            await _unitOfWork.SaveAsync(CancellationToken.None);

            return Result.Success();
        }
    }
}