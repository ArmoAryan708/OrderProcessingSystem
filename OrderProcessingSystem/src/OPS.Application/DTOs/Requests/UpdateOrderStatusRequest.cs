using OPS.Domain.Enums;

namespace OPS.Application.DTOs.Requests;

public record UpdateOrderStatusRequest(
    OrderStatus Status,
    string? Reason = null
);
