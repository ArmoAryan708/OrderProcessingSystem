namespace OPS.Domain.Enums;

public enum OrderStatus
{
    Pending = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}

public static class OrderStatusTransitions
{
    private static readonly Dictionary<OrderStatus, IReadOnlyList<OrderStatus>> _allowed = new()
    {
        [OrderStatus.Pending]    = [OrderStatus.Processing, OrderStatus.Cancelled],
        [OrderStatus.Processing] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped]    = [OrderStatus.Delivered],
        [OrderStatus.Delivered]  = [],
        [OrderStatus.Cancelled]  = []
    };

    public static bool CanTransitionTo(this OrderStatus current, OrderStatus next)
        => _allowed.TryGetValue(current, out var allowed) && allowed.Contains(next);
}
