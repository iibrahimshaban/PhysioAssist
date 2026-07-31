using PhysioAssist.Api.Modules.PatientModule.Entities;

namespace PhysioAssist.Api.Modules.PatientModule.Repositories
{
    public interface IPatientRepo:IBaseRepository<Patient>
    {
        Task<Patient?> GetByEmailAsync(string email);
        Task<Patient?> GetByPhoneAsync(string phoneNumber);
        Task<IEnumerable<Patient>> GetByDoctorId(Guid doctorId, CancellationToken cancellation);
        Task<Patient?> GetByPatientWithFreeTimeSlotsAsync(Guid patientId, CancellationToken cancellation);
    }
}
