using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Interfaces;

namespace PhysioAssist.Api.Shared.Repositories;

public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    private readonly ApplicationDbContext _context = context;
    public void Dispose()
    {
        _context.Dispose();
    }

    public async Task SaveAsync(CancellationToken cancellation) => await _context.SaveChangesAsync(cancellation);
}
