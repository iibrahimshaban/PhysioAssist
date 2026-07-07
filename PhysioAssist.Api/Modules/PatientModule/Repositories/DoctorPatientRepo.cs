using PhysioAssist.Api.Modules.PatientModule.Entities;
using PhysioAssist.Api.Persistence;

namespace PhysioAssist.Api.Modules.PatientModule.Repositories
{
    public class DoctorPatientRepo : IDoctorPatientRepo
    {
        private readonly ApplicationDbContext _context;

        public DoctorPatientRepo(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DoctorPatient?> GetByPatientIdAsync(Guid patientId)
        {
            return await _context.DoctorPatients
                .FirstOrDefaultAsync(dp => dp.PatientId == patientId);
        }

        public async Task<DoctorPatient?> GetByDoctorAndPatientAsync(Guid doctorId, Guid patientId)
        {
            return await _context.DoctorPatients
                .FirstOrDefaultAsync(dp => dp.DoctorId == doctorId && dp.PatientId == patientId);
        }

        public async Task AddAsync(DoctorPatient entity)
        {
            await _context.DoctorPatients.AddAsync(entity);
        }

        public void Update(DoctorPatient entity)
        {
            _context.DoctorPatients.Update(entity);
        }
    }
}
