using PhysioAssist.Api.Modules.PatientModule.Entities;

namespace PhysioAssist.Api.Modules.PatientModule.Repositories
{
    public interface IPatientRepo : IBaseRepository<Patient>
    {
        Task<Patient?> GetByEmailAsync(string email, Guid clinicId);
        Task<Patient?> GetByPhoneAsync(string phoneNumber, Guid clinicId);
        Task<IEnumerable<Patient>> GetByDoctorId(Guid doctorId, CancellationToken cancellation);
        Task<Patient?> GetByPatientWithFreeTimeSlotsAsync(Guid patientId, CancellationToken cancellation);
        Task<IEnumerable<Patient>> GetByClinicIdAsync(Guid clinicId, CancellationToken cancellation);
    }
}
