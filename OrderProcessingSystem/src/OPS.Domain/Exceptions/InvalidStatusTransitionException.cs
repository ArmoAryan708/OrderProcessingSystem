using OPS.Domain.Enums;

namespace OPS.Domain.Exceptions;

public class InvalidStatusTransitionException : Exception
{
    public string ErrorCode => "INVALID_STATUS_TRANSITION";

    public InvalidStatusTransitionException(OrderStatus from, OrderStatus to)
        : base($"Cannot transition order status from '{from}' to '{to}'.")
    {
    }
}
