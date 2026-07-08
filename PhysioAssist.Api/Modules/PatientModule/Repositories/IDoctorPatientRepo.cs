using PhysioAssist.Api.Modules.PatientModule.Entities;

namespace PhysioAssist.Api.Modules.PatientModule.Repositories
{
    public interface IDoctorPatientRepo
    {
        Task<DoctorPatient?> GetByPatientIdAsync(Guid patientId);
        Task<DoctorPatient?> GetByDoctorAndPatientAsync(Guid doctorId, Guid patientId);
        Task AddAsync(DoctorPatient entity);
        void Update(DoctorPatient entity);
    }
}
