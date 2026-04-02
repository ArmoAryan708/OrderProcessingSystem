using OPS.Domain.Enums;

namespace OPS.Application.DTOs.Responses;

public record OrderSummaryResponse(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    OrderStatus Status,
    decimal TotalAmount,
    DateTimeOffset CreatedAt
);
