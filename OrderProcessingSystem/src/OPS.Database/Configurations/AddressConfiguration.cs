using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OPS.Domain.Entities;

namespace OPS.Database.Configurations;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Line1).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Line2).HasMaxLength(200);
        builder.Property(a => a.City).IsRequired().HasMaxLength(100);
        builder.Property(a => a.State).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Zip).IsRequired().HasMaxLength(20);
        builder.Property(a => a.Country).IsRequired().HasMaxLength(100);
        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
