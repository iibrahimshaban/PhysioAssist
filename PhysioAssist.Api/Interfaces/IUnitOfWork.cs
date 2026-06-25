namespace PhysioAssist.Api.Interfaces;

public interface IUnitOfWork : IDisposable
{
    Task SaveAsync();
}
