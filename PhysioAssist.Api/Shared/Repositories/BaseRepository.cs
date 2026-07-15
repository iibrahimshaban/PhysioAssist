using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Interfaces.Common;

namespace PhysioAssist.Api.Shared.Repositories;

public class BaseRepository<T> : IBaseRepository<T> where T : class
{
    protected readonly ApplicationDbContext _context;
    private readonly DbSet<T> _entry;
    public BaseRepository(ApplicationDbContext context)
    {
        _context = context;
        _entry = _context.Set<T>();
    }
    public async Task AddAsync(T entity) 
    {
        await _entry.AddAsync(entity);
    }

    public void Delete(T entity)
    {
        _entry.Remove(entity);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _entry.ToListAsync();
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _entry.FindAsync(id);
    }

    public void Update(T entity)
    {
        _entry.Update(entity);
    }
}
