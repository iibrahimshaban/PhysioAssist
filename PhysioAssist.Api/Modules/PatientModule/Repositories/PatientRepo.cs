using PhysioAssist.Api.Modules.PatientModule.Entities;

namespace PhysioAssist.Api.Modules.PatientModule.Repositories
{
    public class PatientRepo : IPatientRepo
    {
        private readonly ApplicationDbContext _context;

        public PatientRepo(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Patient entity)
        {
            await _context.Patients.AddAsync(entity);
        }

        public void Delete(Patient entity)
        {
            _context.Patients.Remove(entity);
        }

        public async Task<IEnumerable<Patient>> GetAllAsync()
        {
            return await _context.Patients.ToListAsync();
        }

        public async Task<Patient?> GetByIdAsync(Guid id)
        {
            return await _context.Patients.FindAsync(id);
        }

        // CHANGED: added clinicId filter — was previously a global lookup,
        // inconsistent with the (ClinicId, EmailAddress) unique index and
        // the cause of the intake path's cross-clinic auto-link.
        public async Task<Patient?> GetByEmailAsync(string email, Guid clinicId)
        {
            return await _context.Patients
                .Include(p => p.PreferredTimeSlots)
                .FirstOrDefaultAsync(p => p.ClinicId == clinicId && p.EmailAddress == email);
        }

        // CHANGED: added clinicId filter — was previously a global lookup,
        // inconsistent with the (ClinicId, PhoneNumber) unique index.
        public async Task<Patient?> GetByPhoneAsync(string phoneNumber, Guid clinicId)
        {
            return await _context.Patients
                .FirstOrDefaultAsync(p => p.ClinicId == clinicId && p.PhoneNumber == phoneNumber);
        }


        //public IQueryable<Patient> GetPatients_Ordered()
        //{
        //    _context.Patients.OrderBy(p => p.FullName);
        //}

        public void Update(Patient entity)
        {
            _context.Patients.Update(entity);
        }
        public async Task<IEnumerable<Patient>> GetByDoctorId(Guid doctorId, CancellationToken cancellation)
        {
            return await _context.DoctorPatients
                .Where(dp => dp.DoctorId == doctorId)
                .Select(dp => dp.Patient)
                .ToListAsync(cancellation);
        }
        public async Task<Patient?> GetByPatientWithFreeTimeSlotsAsync(Guid patientId, CancellationToken cancellation)
        {
            return await _context.Patients
                .Include(p => p.PreferredTimeSlots)
                .FirstOrDefaultAsync(p => p.Id == patientId, cancellation);
        }
        public async Task<IEnumerable<Patient>> GetByClinicIdAsync(Guid clinicId, CancellationToken cancellation)
        {
            return await _context.Patients
                .Where(p => p.ClinicId == clinicId)
                .ToListAsync(cancellation);
        }
    }
}