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

        public async Task<Patient?> GetByIdAsync(int id)
        {
            return await _context.Patients.FindAsync(id);
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
