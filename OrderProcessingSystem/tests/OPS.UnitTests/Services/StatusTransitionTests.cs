using FluentAssertions;
using OPS.Domain.Enums;

namespace OPS.UnitTests.Services;

public class StatusTransitionTests
{
    [Theory]
    [InlineData(OrderStatus.Pending,    OrderStatus.Processing, true)]
    [InlineData(OrderStatus.Pending,    OrderStatus.Cancelled,  true)]
    [InlineData(OrderStatus.Processing, OrderStatus.Shipped,    true)]
    [InlineData(OrderStatus.Processing, OrderStatus.Cancelled,  true)]
    [InlineData(OrderStatus.Shipped,    OrderStatus.Delivered,  true)]
    public void CanTransitionTo_ValidTransitions_ReturnsTrue(OrderStatus from, OrderStatus to, bool expected)
        => from.CanTransitionTo(to).Should().Be(expected);

    [Theory]
    [InlineData(OrderStatus.Pending,    OrderStatus.Shipped)]
    [InlineData(OrderStatus.Pending,    OrderStatus.Delivered)]
    [InlineData(OrderStatus.Processing, OrderStatus.Pending)]
    [InlineData(OrderStatus.Processing, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Shipped,    OrderStatus.Pending)]
    [InlineData(OrderStatus.Shipped,    OrderStatus.Processing)]
    [InlineData(OrderStatus.Shipped,    OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Delivered,  OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Cancelled,  OrderStatus.Pending)]
    [InlineData(OrderStatus.Cancelled,  OrderStatus.Processing)]
    public void CanTransitionTo_InvalidTransitions_ReturnsFalse(OrderStatus from, OrderStatus to)
        => from.CanTransitionTo(to).Should().BeFalse();
}
