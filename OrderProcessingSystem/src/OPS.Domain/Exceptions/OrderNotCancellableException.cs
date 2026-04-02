using OPS.Domain.Enums;

namespace OPS.Domain.Exceptions;

public class OrderNotCancellableException : Exception
{
    public string ErrorCode => "ORDER_NOT_CANCELLABLE";

    public OrderNotCancellableException(OrderStatus currentStatus)
        : base($"Only PENDING or PROCESSING orders can be cancelled. Current status: {currentStatus}.")
    {
    }
}
