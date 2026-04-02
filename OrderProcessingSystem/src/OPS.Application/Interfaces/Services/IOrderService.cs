using OPS.Application.DTOs.Requests;
using OPS.Application.DTOs.Responses;
using OPS.Domain.Enums;

namespace OPS.Application.Interfaces.Services;

public interface IOrderService
{
    Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<OrderSummaryResponse>> GetAllAsync(OrderStatus? status, CancellationToken cancellationToken = default);
    Task<UpdateStatusResponse> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request, string changedBy, CancellationToken cancellationToken = default);
    Task<CancelOrderResponse> CancelAsync(Guid id, CancelOrderRequest request, CancellationToken cancellationToken = default);
}
