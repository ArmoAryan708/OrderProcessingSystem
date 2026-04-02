using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OPS.Application.Interfaces;
using OPS.Application.Interfaces.Repositories;
using OPS.BackgroundServices.Workers;
using OPS.Domain.Entities;
using OPS.Domain.Enums;

namespace OPS.UnitTests.Workers;

public class OrderProcessingWorkerTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IServiceScope> _scopeMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<OrderProcessingWorker>> _loggerMock = new();
    private readonly OrderProcessingWorker _sut;

    public OrderProcessingWorkerTests()
    {
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceProviderMock.Setup(p => p.GetService(typeof(IOrderRepository))).Returns(_orderRepoMock.Object);
        _serviceProviderMock.Setup(p => p.GetService(typeof(IUnitOfWork))).Returns(_unitOfWorkMock.Object);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _sut = new OrderProcessingWorker(_scopeFactoryMock.Object, _loggerMock.Object);
    }

    private async Task RunWorkerOnceAsync(CancellationTokenSource cts)
    {
        try
        {
            await _sut.StartAsync(cts.Token);
            await _sut.ExecuteTask!;
        }
        catch (OperationCanceledException) { }
    }

    [Fact]
    public async Task ProcessPendingOrders_WhenNoPendingOrders_DoesNotCommit()
    {
        var cts = new CancellationTokenSource();
        _orderRepoMock
            .Setup(r => r.GetPendingOrdersAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(_ => cts.Cancel())
            .ReturnsAsync(new List<Order>());

        await RunWorkerOnceAsync(cts);

        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPendingOrders_WhenPendingOrdersExist_TransitionsEachOrderToProcessing()
    {
        var cts = new CancellationTokenSource();
        var orders = new List<Order>
        {
            new() { Id = Guid.NewGuid(), Status = OrderStatus.Pending },
            new() { Id = Guid.NewGuid(), Status = OrderStatus.Pending },
        };

        _orderRepoMock
            .Setup(r => r.GetPendingOrdersAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(_ => cts.Cancel())
            .ReturnsAsync(orders);

        _orderRepoMock
            .Setup(r => r.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<OrderStatus>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await RunWorkerOnceAsync(cts);

        foreach (var order in orders)
        {
            _orderRepoMock.Verify(r => r.UpdateStatusAsync(
                order.Id,
                OrderStatus.Processing,
                "BackgroundJob",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task ProcessPendingOrders_WhenPendingOrdersExist_CommitsOnce()
    {
        var cts = new CancellationTokenSource();
        var orders = new List<Order>
        {
            new() { Id = Guid.NewGuid(), Status = OrderStatus.Pending },
            new() { Id = Guid.NewGuid(), Status = OrderStatus.Pending },
        };

        _orderRepoMock
            .Setup(r => r.GetPendingOrdersAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(_ => cts.Cancel())
            .ReturnsAsync(orders);

        _orderRepoMock
            .Setup(r => r.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<OrderStatus>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await RunWorkerOnceAsync(cts);

        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPendingOrders_WhenRepositoryThrows_DoesNotCommit()
    {
        var cts = new CancellationTokenSource();

        _orderRepoMock
            .Setup(r => r.GetPendingOrdersAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(_ => cts.Cancel())
            .ThrowsAsync(new InvalidOperationException("DB error"));

        await RunWorkerOnceAsync(cts);

        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPendingOrders_WhenRepositoryThrows_DoesNotUpdateAnyOrders()
    {
        var cts = new CancellationTokenSource();

        _orderRepoMock
            .Setup(r => r.GetPendingOrdersAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(_ => cts.Cancel())
            .ThrowsAsync(new InvalidOperationException("DB error"));

        await RunWorkerOnceAsync(cts);

        _orderRepoMock.Verify(r => r.UpdateStatusAsync(
            It.IsAny<Guid>(), It.IsAny<OrderStatus>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
