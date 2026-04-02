using Microsoft.EntityFrameworkCore;
using OPS.Application.Interfaces.Repositories;
using OPS.Database.DbContext;
using OPS.Domain.Entities;

namespace OPS.Database.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context) => _context = context;

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Products.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        => await _context.Products.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);
}
