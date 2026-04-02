using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OPS.Application.Interfaces;
using OPS.Application.Interfaces.Repositories;
using OPS.Domain.Enums;

namespace OPS.BackgroundServices.Workers;

public class OrderProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderProcessingWorker> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    public OrderProcessingWorker(IServiceScopeFactory scopeFactory, ILogger<OrderProcessingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderProcessingWorker started. Interval: {Interval}", Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingOrdersAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessPendingOrdersAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            var pendingOrders = await orderRepository.GetPendingOrdersAsync(stoppingToken);

            if (pendingOrders.Count == 0)
            {
                _logger.LogInformation("No PENDING orders to process.");
                return;
            }

            foreach (var order in pendingOrders)
            {
                await orderRepository.UpdateStatusAsync(
                    order.Id,
                    OrderStatus.Processing,
                    changedBy: "BackgroundJob",
                    reason: "Automatically promoted by background job",
                    stoppingToken);
            }

            await unitOfWork.CommitAsync(stoppingToken);
            _logger.LogInformation("Promoted {Count} PENDING orders to PROCESSING.", pendingOrders.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "OrderProcessingWorker failed: {Message}", ex.Message);
        }
    }
}
