namespace OPS.Domain.Entities;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }  // Snapshot of price at time of order

    public decimal Subtotal => UnitPrice * Quantity;

    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
