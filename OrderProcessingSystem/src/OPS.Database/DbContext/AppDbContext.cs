using Microsoft.EntityFrameworkCore;
using OPS.Domain.Entities;

namespace OPS.Database.DbContext;

public class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Domain.Entities.BaseEntity &&
                        e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            var entity = (Domain.Entities.BaseEntity)entry.Entity;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            if (entry.State == EntityState.Added)
                entity.CreatedAt = DateTimeOffset.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
