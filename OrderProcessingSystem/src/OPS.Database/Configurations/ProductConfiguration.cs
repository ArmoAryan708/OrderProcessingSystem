using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OPS.Domain.Entities;

namespace OPS.Database.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
