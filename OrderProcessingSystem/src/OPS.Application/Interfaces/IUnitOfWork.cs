namespace OPS.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
