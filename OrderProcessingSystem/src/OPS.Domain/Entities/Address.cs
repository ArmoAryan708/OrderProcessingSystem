namespace OPS.Domain.Entities;

public class Address : BaseEntity
{
    public Guid CustomerId { get; set; }
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    public Customer Customer { get; set; } = null!;
    public ICollection<Order> Orders { get; set; } = [];
}
