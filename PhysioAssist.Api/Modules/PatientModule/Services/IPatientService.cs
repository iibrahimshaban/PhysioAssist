using PhysioAssist.Api.Modules.PatientModule.DTOs;

namespace PhysioAssist.Api.Modules.PatientModule.Services
{
    public interface IPatientService
    {
        Task<Result<PatientResponse>> GetByIdAsync(Guid patientId);
        Task<Result<IEnumerable<PatientResponse>>> GetAllAsync();
        Task<Result<PatientResponse>> CreateAsync(PatientRequest request);
        Task<Result<PatientResponse>> UpdateAsync(Guid patientId, PatientRequest request);
        Task<Result> DeleteAsync(Guid patientId);
        Task<Result> UpdateStatusAsync(Guid patientId, PatientStatus status);

        // patient doctor 

        Task<Result> AssignPatientAsync(Guid doctorId, Guid patientId);
        Task<Result> DischargePatientAsync(Guid doctorId, Guid patientId);
        Task<Result> SetPrimaryDoctorAsync(Guid doctorId, Guid patientId);

        // patient slot 
        Task<Result<IEnumerable<PatientWithNextSlotResponse>>> GetPatientsWithSlotsAsync(Guid doctorId, CancellationToken ct = default);

        Task<Result<PatientOverviewResponse>> GetPatientOverviewAsync(Guid patientId, CancellationToken ct = default);


        Task<Result> UpdatePatientOverviewSubmissionAsync(Guid patientId, string formSubmissionData, CancellationToken ct = default);
    }
}