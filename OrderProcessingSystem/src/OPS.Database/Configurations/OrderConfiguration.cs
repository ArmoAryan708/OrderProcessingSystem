using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OPS.Domain.Entities;
using OPS.Domain.Enums;

namespace OPS.Database.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(o => o.CancelReason).HasMaxLength(500);

        builder.HasQueryFilter(o => !o.IsDeleted);

        builder.HasOne(o => o.Customer).WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.ShippingAddress).WithMany(a => a.Orders)
            .HasForeignKey(o => o.ShippingAddressId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(o => o.Items).WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}
