using OPS.Domain.Enums;

namespace OPS.Domain.Entities;

public class Order : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Guid ShippingAddressId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }

    public Customer Customer { get; set; } = null!;
    public Address ShippingAddress { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = [];
}
