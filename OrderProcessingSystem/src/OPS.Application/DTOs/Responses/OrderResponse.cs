using OPS.Domain.Enums;

namespace OPS.Application.DTOs.Responses;

public record OrderResponse(
    Guid Id,
    OrderStatus Status,
    decimal TotalAmount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    CustomerBriefResponse Customer,
    AddressResponse ShippingAddress,
    IReadOnlyList<OrderItemResponse> Items
);

public record OrderItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal
);

public record CustomerBriefResponse(Guid Id, string Name, string Email);

public record AddressResponse(
    Guid Id,
    string Line1,
    string? Line2,
    string City,
    string State,
    string Zip,
    string Country
);
