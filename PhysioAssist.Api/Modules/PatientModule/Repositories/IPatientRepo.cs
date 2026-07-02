using PhysioAssist.Api.Modules.PatientModule.Entities;
using PhysioAssist.Api.Shared.Interfaces;

namespace PhysioAssist.Api.Modules.PatientModule.Repositories
{
    public interface IPatientRepo:IBaseRepository<Patient>
    {
        //Task<Patient?> GetByIdAsync(Guid patientId);
        //Task UpdatePatientAsync(Patient patient);
        //Task DeletePatientAsync(Patient patient);
        //Task AddPatientAsync(Patient patient);

        //IQueryable<Patient> GetPatients_Ordered();
    }
}
