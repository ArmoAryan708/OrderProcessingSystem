using Microsoft.EntityFrameworkCore;
using OPS.Database.DbContext;
using OPS.Domain.Entities;

namespace OPS.Database.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Products.AnyAsync()) return;

        var products = new List<Product>
        {
            new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111101"), Name = "Laptop Pro 15",        Description = "High-performance laptop",  Price = 1299.99m, Stock = 50 },
            new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111102"), Name = "Wireless Mouse",       Description = "Ergonomic wireless mouse",  Price = 29.99m,   Stock = 200 },
            new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111103"), Name = "Mechanical Keyboard",  Description = "RGB mechanical keyboard",   Price = 89.99m,   Stock = 150 },
            new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111104"), Name = "4K Monitor",           Description = "27-inch 4K display",        Price = 549.99m,  Stock = 75 },
            new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111105"), Name = "USB-C Hub",            Description = "7-in-1 USB-C hub",          Price = 49.99m,   Stock = 300 }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }
}
