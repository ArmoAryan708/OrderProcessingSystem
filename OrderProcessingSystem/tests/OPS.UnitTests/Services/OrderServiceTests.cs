using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using OPS.Application.DTOs.Requests;
using OPS.Application.Interfaces;
using OPS.Application.Interfaces.Repositories;
using OPS.Application.Services;
using OPS.Domain.Entities;
using OPS.Domain.Enums;
using OPS.Domain.Exceptions;

namespace OPS.UnitTests.Services;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IValidator<CreateOrderRequest>> _validatorMock = new();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<CreateOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _sut = new OrderService(
            _orderRepoMock.Object,
            _productRepoMock.Object,
            _customerRepoMock.Object,
            _unitOfWorkMock.Object,
            _validatorMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WhenCustomerNotFound_ThrowsNotFoundException()
    {
        _customerRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Customer?)null);

        var request = new CreateOrderRequest(Guid.NewGuid(), Guid.NewGuid(), [new OrderItemRequest(Guid.NewGuid(), 1)]);

        await _sut.Invoking(s => s.CreateAsync(request))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Customer*");
    }

    [Fact]
    public async Task CreateAsync_WhenProductNotFound_ThrowsNotFoundException()
    {
        var customerId = Guid.NewGuid();
        var addressId = Guid.NewGuid();
        var missingProductId = Guid.NewGuid();

        var customer = new Customer { Id = customerId, Name = "Test", Email = "t@t.com", PasswordHash = "h" };
        customer.Addresses.Add(new Address { Id = addressId, CustomerId = customerId });

        _customerRepoMock.Setup(r => r.GetByIdAsync(customerId, default))
            .ReturnsAsync(customer);

        _productRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync([]);

        var request = new CreateOrderRequest(customerId, addressId, [new OrderItemRequest(missingProductId, 1)]);

        await _sut.Invoking(s => s.CreateAsync(request))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Product*");
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderNotFound_ThrowsNotFoundException()
    {
        _orderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Order?)null);

        await _sut.Invoking(s => s.GetByIdAsync(Guid.NewGuid()))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Order*");
    }

    [Fact]
    public async Task UpdateStatusAsync_InvalidTransition_ThrowsInvalidStatusTransitionException()
    {
        var orderId = Guid.NewGuid();
        var order = new Order { Id = orderId, Status = OrderStatus.Delivered };

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId, default)).ReturnsAsync(order);

        var request = new UpdateOrderStatusRequest(OrderStatus.Pending);

        await _sut.Invoking(s => s.UpdateStatusAsync(orderId, request, "admin"))
            .Should().ThrowAsync<InvalidStatusTransitionException>();
    }

    [Fact]
    public async Task UpdateStatusAsync_ValidTransition_ReturnsUpdatedStatus()
    {
        var orderId = Guid.NewGuid();
        var order = new Order { Id = orderId, Status = OrderStatus.Processing };

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId, default)).ReturnsAsync(order);
        _unitOfWorkMock.Setup(u => u.CommitAsync(default)).ReturnsAsync(1);

        var request = new UpdateOrderStatusRequest(OrderStatus.Shipped);
        var result = await _sut.UpdateStatusAsync(orderId, request, "admin");

        result.PreviousStatus.Should().Be(OrderStatus.Processing);
        result.CurrentStatus.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public async Task CancelAsync_WhenOrderIsDelivered_ThrowsOrderNotCancellableException()
    {
        var orderId = Guid.NewGuid();
        var order = new Order { Id = orderId, Status = OrderStatus.Delivered };

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId, default)).ReturnsAsync(order);

        await _sut.Invoking(s => s.CancelAsync(orderId, new CancelOrderRequest()))
            .Should().ThrowAsync<OrderNotCancellableException>();
    }

    [Fact]
    public async Task CancelAsync_WhenOrderIsPending_Succeeds()
    {
        var orderId = Guid.NewGuid();
        var order = new Order { Id = orderId, Status = OrderStatus.Pending };

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId, default)).ReturnsAsync(order);
        _unitOfWorkMock.Setup(u => u.CommitAsync(default)).ReturnsAsync(1);

        var result = await _sut.CancelAsync(orderId, new CancelOrderRequest("Test reason"));

        result.Status.Should().Be("CANCELLED");
        result.Reason.Should().Be("Test reason");
    }
}
