using PhysioAssist.Api.Modules.Scheduling.Entities;

namespace PhysioAssist.Api.Modules.Scheduling.Repositories.Interfaces
{
    public interface IGuestRepository : IBaseRepository<Guest>
    {
        Task<Guest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<Guest>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    }
}
