using Microsoft.EntityFrameworkCore;
using OPS.Application.DTOs.Responses;
using OPS.Application.Interfaces.Repositories;
using OPS.Database.DbContext;
using OPS.Domain.Entities;
using OPS.Domain.Enums;

namespace OPS.Database.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context) => _context = context;

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.ShippingAddress)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<List<OrderSummaryResponse>> GetAllAsync(OrderStatus? status, CancellationToken cancellationToken = default)
    {
        var query = _context.Orders
            .Include(o => o.Customer)
            .Where(o => !o.IsDeleted);

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderSummaryResponse(o.Id, o.CustomerId, o.Customer.Name, o.Status, o.TotalAmount, o.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
        => await _context.Orders.AddAsync(order, cancellationToken);

    public async Task UpdateStatusAsync(Guid id, OrderStatus newStatus, string changedBy, string? reason, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order is null) return;

        order.Status = newStatus;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        if (newStatus == OrderStatus.Cancelled)
        {
            order.CancelledAt = DateTimeOffset.UtcNow;
            order.CancelReason = reason;
        }
    }

    public async Task<IReadOnlyList<Order>> GetPendingOrdersAsync(CancellationToken cancellationToken = default)
        => await _context.Orders
            .Where(o => o.Status == OrderStatus.Pending && !o.IsDeleted)
            .ToListAsync(cancellationToken);
}
