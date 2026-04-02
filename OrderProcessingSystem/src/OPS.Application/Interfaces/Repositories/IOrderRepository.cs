using OPS.Application.DTOs.Responses;
using OPS.Domain.Entities;
using OPS.Domain.Enums;

namespace OPS.Application.Interfaces.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<OrderSummaryResponse>> GetAllAsync(OrderStatus? status, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid id, OrderStatus newStatus, string changedBy, string? reason, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetPendingOrdersAsync(CancellationToken cancellationToken = default);
}
