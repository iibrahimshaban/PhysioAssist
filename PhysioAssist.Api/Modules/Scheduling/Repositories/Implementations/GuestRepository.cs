using PhysioAssist.Api.Modules.Scheduling.DTO.Guest;
using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.Repositories.Interfaces;

namespace PhysioAssist.Api.Modules.Scheduling.Repositories.Implementations
{

    namespace PhysioAssist.Api.Modules.Scheduling.Repositories.Implementations
    {
        public class GuestRepository : BaseRepository<Guest>, IGuestRepository
        {
            private readonly ApplicationDbContext _context;

            public GuestRepository(ApplicationDbContext context) : base(context)
            {
                _context = context;
            }

            public async Task<Guest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            {
                return await _context.Guests.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
            }

            public async Task<List<Guest>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
            {
                var idList = ids.ToList();
                if (idList.Count == 0) return [];

                return await _context.Guests
                    .Where(g => idList.Contains(g.Id))
                    .ToListAsync(cancellationToken);
            }
        }
    }
}
