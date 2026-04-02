using OPS.Application.Interfaces;
using OPS.Database.DbContext;

namespace OPS.Database.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context) => _context = context;

    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public void Dispose() => _context.Dispose();
}
