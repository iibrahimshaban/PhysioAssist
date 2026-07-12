using PhysioAssist.Api.Modules.PatientModule.Entities;
using PhysioAssist.Api.Persistence;

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

        public async Task<Patient?> GetByEmailAsync(string email)
        {
            return await _context.Patients.FirstOrDefaultAsync(p => p.EmailAddress == email);
        }

        public async Task<Patient?> GetByPhoneAsync(string phoneNumber)
        {
            return await _context.Patients
                .FirstOrDefaultAsync(p => p.PhoneNumber == phoneNumber);
        }


        //public IQueryable<Patient> GetPatients_Ordered()
        //{
        //    _context.Patients.OrderBy(p => p.FullName);
        //}

        public void Update(Patient entity)
        {
            _context.Patients.Update(entity);
        }
    }
}
