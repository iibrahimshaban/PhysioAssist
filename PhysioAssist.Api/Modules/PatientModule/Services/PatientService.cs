using PhysioAssist.Api.Modules.Intake.QueryServices;
using PhysioAssist.Api.Modules.PatientModule.DTOs;
using PhysioAssist.Api.Modules.PatientModule.Entities;
using PhysioAssist.Api.Modules.PatientModule.Errors;
using PhysioAssist.Api.Modules.PatientModule.Repositories;
using PhysioAssist.Api.Modules.PatientModule.Helpers;


namespace PhysioAssist.Api.Modules.PatientModule.Services
{
    public class PatientService : IPatientService
    {
        private readonly IPatientRepo _patientRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDoctorPatientRepo _doctorPatientRepo;
        private readonly IScheduleSlotQueryService _scheduleSlotQueryService;
        private readonly ApplicationDbContext _context;
        private readonly IPatientOverviewIntakeQueryService _overviewIntakeQueryService;
        private readonly IPatientOverviewIntakeCommandService _overviewIntakeCommandService;
        private readonly IIntakeCreationQueryService _intakeCreationQueryService;
        private readonly IIntakeConversionMarkerService _intakeConversionMarkerService;
        private readonly IPatientQueryService _patientQueryService;



        public PatientService(
            IPatientRepo patientRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IDoctorPatientRepo doctorPatientRepo,
            IScheduleSlotQueryService scheduleSlotQueryService,
            IPatientOverviewIntakeQueryService overviewIntakeQueryService,
            IPatientOverviewIntakeCommandService overviewIntakeCommandService,
            IIntakeCreationQueryService intakeCreationQueryService,
            IIntakeConversionMarkerService intakeConversionMarkerService,
            IPatientQueryService patientQueryService,
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _patientRepo = patientRepo;
            _unitOfWork = unitOfWork;
            _patientQueryService = patientQueryService;
            _doctorPatientRepo = doctorPatientRepo;
            _scheduleSlotQueryService = scheduleSlotQueryService;
            _overviewIntakeQueryService = overviewIntakeQueryService;
            _overviewIntakeCommandService = overviewIntakeCommandService;
            _intakeCreationQueryService = intakeCreationQueryService;
            _intakeConversionMarkerService = intakeConversionMarkerService;
            _context = context;
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

        public async Task<Result<IEnumerable<PatientWithNextSlotResponse>>> GetPatientsWithSlotsAsync(Guid doctorId, CancellationToken ct = default)
        {

            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.Id == doctorId, ct);

            if (doctor is null)
                return Result.Failure<IEnumerable<PatientWithNextSlotResponse>>(PatientErrors.NotADoctor);

            // 3. Get today's slots for this doctor
            var slots = await _scheduleSlotQueryService.GetUpcomingSlotsForDoctorAsync(doctor.Id, ct);

            // 4. Get all patients
            var patients = await _patientRepo.GetByDoctorId(doctorId, ct);

            // 5. Build slot lookup
            var slotLookup = slots
                .GroupBy(s => s.PatientId)
                .ToDictionary(g => g.Key, g => g.OrderBy(s => s.SlotStart).First());

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

        public async Task<Result<PatientOverviewResponse>> GetPatientOverviewAsync(Guid patientId, CancellationToken ct = default)
        {
            var patient = await _patientRepo.GetByIdAsync(patientId);
            if (patient is null)
                return Result.Failure<PatientOverviewResponse>(PatientErrors.NotFound);

            var response = patient.Adapt<PatientOverviewResponse>();

            var overviewResult = await _overviewIntakeQueryService.GetOverviewDataForPatientAsync(patientId, ct);
            if (overviewResult.IsSuccess)
            {
                response.FormSubmissionData = overviewResult.Value.FormSubmissionData;
                response.PainPointsJson = overviewResult.Value.PainPointsJson;
                response.DoctorInfoJson = overviewResult.Value.DoctorInfoJson;
            }

            return Result.Success(response);
        }


        public async Task<Result> UpdatePatientOverviewSubmissionAsync(
            Guid patientId,
            string formSubmissionData,
            string? painPointsData,
            CancellationToken ct = default)
        {
            return await _overviewIntakeCommandService.UpdateOverviewDataAsync(
                patientId, formSubmissionData, painPointsData, ct);
        }

        public async Task<Result<Guid>> CreatePatientFromDynamicFormAsync(
            Guid formSchemaId, string formSubmissionData, string? painPointsData, Guid doctorId, CancellationToken ct = default)
        {
            // Step 1 — extract fields using Patient module's own helper (no DB writes yet, no dependency on Intake's DTOs)
            using var submissionDoc = PatientIntakeExtractionHelper.ParseSubmissionJson(formSubmissionData);
            if (submissionDoc is null)
                return Result.Failure<Guid>(PatientErrors.InvalidIntakeSubmission);

            var root = submissionDoc.RootElement;

            var fullName = PatientIntakeExtractionHelper.ExtractAnswerString(root, IntakeQuestionIds.FullName, "text");
            var email = PatientIntakeExtractionHelper.ExtractAnswerString(root, IntakeQuestionIds.Email, "email");
            var phone = PatientIntakeExtractionHelper.ExtractAnswerString(root, IntakeQuestionIds.Phone, "phone");
            var gender = PatientIntakeExtractionHelper.ExtractAnswerString(root, IntakeQuestionIds.Gender, "radio");
            DateTime? dateOfBirth = PatientIntakeExtractionHelper.ExtractAnswerDate(root, IntakeQuestionIds.DateOfBirth, "date");
            var freeTime = PatientIntakeExtractionHelper.ExtractAnswerString(root, IntakeQuestionIds.FreeTime, "text");
            var caseNotes = PatientIntakeExtractionHelper.ExtractAnswerString(root, IntakeQuestionIds.ChiefComplaint, "text");


            var raw = PatientIntakeExtractionHelper.ExtractAnswerString(root, IntakeQuestionIds.PatientType, "select");
            var patientCategory = Enum.TryParse<PatientCategory>(raw, ignoreCase: true, out var category) ? category : PatientCategory.GeneralOther;
            

            if (string.IsNullOrWhiteSpace(fullName))
                return Result.Failure<Guid>(PatientErrors.InvalidIntakeSubmission);

            var createPatientResult = await _patientQueryService.CreatePatientFromIntakeAsync(
                new PhysioAssist.Api.Shared.Dtos.Patient.CreatePatientFromIntakeRequest(
                    fullName,
                    email,
                    phone,
                    gender,
                    dateOfBirth,
                    doctorId,
                    patientCategory,
                    freeTime,
                    caseNotes),
                ct);

            if (createPatientResult.IsFailure)
                return Result.Failure<Guid>(createPatientResult.Error);

            var patientId = createPatientResult.Value;

            // Step 3 — only now, after the patient exists, create the intake row via the exposed Intake function
            var createIntakeResult = await _intakeCreationQueryService.CreateDirectIntakeAsync(
                formSchemaId, formSubmissionData, painPointsData, doctorId, ct);

            if (createIntakeResult.IsFailure)
                return Result.Failure<Guid>(createIntakeResult.Error);

            var intakeId = createIntakeResult.Value;

            // Step 4 — wire the intake to the patient via the exposed Intake function
            var markResult = await _intakeConversionMarkerService.MarkIntakeConvertedAsync(intakeId, patientId, doctorId, ct);
            if (markResult.IsFailure)
                return Result.Failure<Guid>(markResult.Error);

            return Result.Success(patientId);
        }
    }
}