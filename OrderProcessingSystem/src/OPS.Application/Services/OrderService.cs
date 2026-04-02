using FluentValidation;
using OPS.Application.DTOs.Requests;
using OPS.Application.DTOs.Responses;
using OPS.Application.Interfaces;
using OPS.Application.Interfaces.Repositories;
using OPS.Application.Interfaces.Services;
using OPS.Domain.Entities;
using OPS.Domain.Enums;
using OPS.Domain.Exceptions;

namespace OPS.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateOrderRequest> _createOrderValidator;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateOrderRequest> createOrderValidator)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _createOrderValidator = createOrderValidator;
    }

    public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createOrderValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new OPS.Domain.Exceptions.ValidationException(validation.ToDictionary());

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException("Customer", request.CustomerId);

        if (!customer.Addresses.Any(a => a.Id == request.ShippingAddressId))
            throw new NotFoundException("Address", request.ShippingAddressId);

        var productIds = request.Items.Select(i => i.ProductId).Distinct();
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
        var productMap = products.ToDictionary(p => p.Id);

        foreach (var item in request.Items)
        {
            if (!productMap.ContainsKey(item.ProductId))
                throw new NotFoundException("Product", item.ProductId);

            var product = productMap[item.ProductId];
            if (item.Quantity > product.Stock)
                throw new OPS.Domain.Exceptions.ValidationException(new Dictionary<string, string[]>
                {
                    [$"Items.Quantity"] = [$"Requested quantity ({item.Quantity}) exceeds available stock ({product.Stock}) for product '{product.Name}'."]
                });
        }

        var order = new Order
        {
            CustomerId = request.CustomerId,
            ShippingAddressId = request.ShippingAddressId,
            Status = OrderStatus.Pending
        };

        var items = request.Items.Select(i =>
        {
            var product = productMap[i.ProductId];
            return new OrderItem
            {
                OrderId = order.Id,
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = product.Price
            };
        }).ToList();

        order.Items = items;
        order.TotalAmount = items.Sum(i => i.UnitPrice * i.Quantity);

        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        var created = await _orderRepository.GetByIdAsync(order.Id, cancellationToken);
        return MapToOrderResponse(created!);
    }

    public async Task<OrderResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Order", id);
        return MapToOrderResponse(order);
    }

    public async Task<List<OrderSummaryResponse>> GetAllAsync(OrderStatus? status, CancellationToken cancellationToken = default)
        => await _orderRepository.GetAllAsync(status, cancellationToken);

    public async Task<UpdateStatusResponse> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request, string changedBy, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Order", id);

        if (!order.Status.CanTransitionTo(request.Status))
            throw new InvalidStatusTransitionException(order.Status, request.Status);

        var previousStatus = order.Status;
        await _orderRepository.UpdateStatusAsync(id, request.Status, changedBy, request.Reason, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return new UpdateStatusResponse(id, previousStatus, request.Status, DateTimeOffset.UtcNow);
    }

    public async Task<CancelOrderResponse> CancelAsync(Guid id, CancelOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Order", id);

        if (order.Status != OrderStatus.Pending)
            throw new OrderNotCancellableException(order.Status);

        await _orderRepository.UpdateStatusAsync(id, OrderStatus.Cancelled, "customer", request.Reason, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return new CancelOrderResponse(id, "CANCELLED", DateTimeOffset.UtcNow, request.Reason);
    }

    private static OrderResponse MapToOrderResponse(Order order) => new(
        order.Id,
        order.Status,
        order.TotalAmount,
        order.CreatedAt,
        order.UpdatedAt,
        order.CancelledAt,
        order.CancelReason,
        new CustomerBriefResponse(order.Customer.Id, order.Customer.Name, order.Customer.Email),
        new AddressResponse(
            order.ShippingAddress.Id,
            order.ShippingAddress.Line1,
            order.ShippingAddress.Line2,
            order.ShippingAddress.City,
            order.ShippingAddress.State,
            order.ShippingAddress.Zip,
            order.ShippingAddress.Country),
        order.Items.Select(i => new OrderItemResponse(
            i.Id,
            i.ProductId,
            i.Product.Name,
            i.Quantity,
            i.UnitPrice,
            i.Subtotal)).ToList()
    );
}
