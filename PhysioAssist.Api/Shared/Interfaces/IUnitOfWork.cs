namespace PhysioAssist.Api.Shared.Interfaces;

public interface IUnitOfWork : IDisposable
{
    Task SaveAsync(CancellationToken cancellationToken= default);
}
