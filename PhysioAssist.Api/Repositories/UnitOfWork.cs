using PhysioAssist.Api.Interfaces;
using PhysioAssist.Api.Persistence;

namespace PhysioAssist.Api.Repositories;

public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    private readonly ApplicationDbContext _context = context;
    public void Dispose()
    {
        _context.Dispose();
    }

    public async Task SaveAsync() => await _context.SaveChangesAsync();
}
