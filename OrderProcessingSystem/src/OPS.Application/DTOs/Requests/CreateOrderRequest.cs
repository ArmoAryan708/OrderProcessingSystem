namespace OPS.Application.DTOs.Requests;

public record CreateOrderRequest(
    Guid CustomerId,
    Guid ShippingAddressId,
    IReadOnlyList<OrderItemRequest> Items
);

public record OrderItemRequest(
    Guid ProductId,
    int Quantity
);
