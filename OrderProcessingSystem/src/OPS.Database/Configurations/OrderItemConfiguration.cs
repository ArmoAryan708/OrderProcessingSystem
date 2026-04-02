using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OPS.Domain.Entities;

namespace OPS.Database.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Ignore(i => i.Subtotal);

        builder.HasQueryFilter(i => !i.IsDeleted);

        builder.HasOne(i => i.Product).WithMany(p => p.OrderItems)
            .HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
