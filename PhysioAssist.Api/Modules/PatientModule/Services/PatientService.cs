using Mapster;
using MapsterMapper;
using PhysioAssist.Api.Modules.PatientModule.DTOs;
using PhysioAssist.Api.Modules.PatientModule.Entities;
using PhysioAssist.Api.Modules.PatientModule.Errors;
using PhysioAssist.Api.Modules.PatientModule.Repositories;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Interfaces;
using System.Security.Claims;

namespace PhysioAssist.Api.Modules.PatientModule.Services
{
    public class PatientService : IPatientService
    {
        private readonly IPatientRepo _patientRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IDoctorPatientRepo _doctorPatientRepo;
        private readonly IScheduleSlotQueryService _scheduleSlotQueryService;
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PatientService(
            IPatientRepo patientRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IDoctorPatientRepo doctorPatientRepo,
            IScheduleSlotQueryService scheduleSlotQueryService,
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _patientRepo = patientRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _doctorPatientRepo = doctorPatientRepo;
            _scheduleSlotQueryService = scheduleSlotQueryService;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
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

        public async Task<Result<IEnumerable<PatientWithNextSlotResponse>>> GetPatientsWithSlotsAsync(CancellationToken ct = default)
        {
            // 1. Get userId from JWT sub claim
            var userId = _httpContextAccessor.HttpContext?.User
    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Result.Failure<IEnumerable<PatientWithNextSlotResponse>>(PatientErrors.Unauthorized);

            // 2. Resolve userId → doctorId
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.UserId == userId, ct);

            if (doctor is null)
                return Result.Failure<IEnumerable<PatientWithNextSlotResponse>>(PatientErrors.NotADoctor);

            // 3. Get today's slots for this doctor
            var slots = await _scheduleSlotQueryService.GetTodaySlotsForDoctorAsync(doctor.Id, ct);

            // 4. Get all patients
            var patients = await _patientRepo.GetAllAsync();

            // 5. Build slot lookup
            var slotLookup = slots.ToDictionary(s => s.PatientId);

            // 6. Merge and order
            var result = patients
                .Select(p =>
                {
                    var response = p.Adapt<PatientWithNextSlotResponse>();
                    if (slotLookup.TryGetValue(p.Id, out var slot))
                    {
                        response.SlotStart = slot.SlotStart;
                        response.SlotEnd = slot.SlotEnd;
                    }
                    return response;
                })
                .OrderBy(p => p.SlotStart.HasValue ? 0 : 1)
                .ThenBy(p => p.SlotStart)
                .ToList();

            return Result.Success<IEnumerable<PatientWithNextSlotResponse>>(result);
        }
    }
}