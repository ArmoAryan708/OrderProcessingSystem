namespace OPS.Domain.Entities;

public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;

    public ICollection<Address> Addresses { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
}
