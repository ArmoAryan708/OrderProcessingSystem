using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OPS.Domain.Entities;

namespace OPS.Database.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(200);
        builder.HasIndex(c => c.Email).IsUnique();
        builder.Property(c => c.Phone).HasMaxLength(50);
        builder.Property(c => c.PasswordHash).IsRequired().HasMaxLength(255);
        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.HasMany(c => c.Addresses).WithOne(a => a.Customer)
            .HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(c => c.Orders).WithOne(o => o.Customer)
            .HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}
