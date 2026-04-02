using Microsoft.EntityFrameworkCore;
using OPS.Application.Interfaces.Repositories;
using OPS.Database.DbContext;
using OPS.Domain.Entities;

namespace OPS.Database.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context) => _context = context;

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Customers.Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _context.Customers.FirstOrDefaultAsync(c => c.Email == email, cancellationToken);

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Customers.Include(c => c.Addresses).ToListAsync(cancellationToken);

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
        => await _context.Customers.AddAsync(customer, cancellationToken);

    public async Task AddAddressAsync(Address address, CancellationToken cancellationToken = default)
        => await _context.Addresses.AddAsync(address, cancellationToken);
}
