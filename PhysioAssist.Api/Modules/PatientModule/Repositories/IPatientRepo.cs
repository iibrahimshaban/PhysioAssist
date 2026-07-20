using PhysioAssist.Api.Modules.PatientModule.DTOs;
using PhysioAssist.Api.Modules.PatientModule.Entities;
using PhysioAssist.Api.Shared.Interfaces.Common;

namespace PhysioAssist.Api.Modules.PatientModule.Repositories
{
    public interface IPatientRepo:IBaseRepository<Patient>
    {
        //Task<Patient?> GetByIdAsync(Guid patientId);
        //Task UpdatePatientAsync(Patient patient);
        //Task DeletePatientAsync(Patient patient);
        //Task AddPatientAsync(Patient patient);
        Task<Patient> GetByEmailAsync(string email);
        Task<Patient?> GetByPhoneAsync(string phoneNumber);
        Task<IEnumerable<Patient>> GetByDoctorId(Guid doctorId, CancellationToken cancellation);
        //IQueryable<Patient> GetPatients_Ordered();
    }
}
