using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OPS.Application.DTOs.Requests;
using OPS.Application.DTOs.Responses;
using OPS.Application.Interfaces.Services;
using OPS.Domain.Enums;

namespace OPS.Web.Controllers;

/// <summary>Order management endpoints</summary>
[ApiController]
[Route("api/v1/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService) => _orderService = orderService;

    private string CurrentUser => User.FindFirstValue(ClaimTypes.Name) ?? "customer";

    /// <summary>Create a new order</summary>
    /// <response code="201">Order created successfully</response>
    /// <response code="400">Validation error</response>
    /// <response code="404">Customer or product not found</response>
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await _orderService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    /// <summary>Get order by ID</summary>
    /// <response code="200">Order found</response>
    /// <response code="404">Order not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(id, cancellationToken);
        return Ok(order);
    }

    /// <summary>List all orders with optional status filter</summary>
    /// <response code="200">Orders retrieved</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<OrderSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] OrderStatus? status,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetAllAsync(status, cancellationToken);
        return Ok(result);
    }

    /// <summary>Update order status</summary>
    /// <response code="200">Status updated</response>
    /// <response code="404">Order not found</response>
    /// <response code="422">Invalid status transition</response>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(UpdateStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _orderService.UpdateStatusAsync(id, request, CurrentUser, cancellationToken);
        return Ok(result);
    }

    /// <summary>Cancel an order (only PENDING orders)</summary>
    /// <response code="200">Order cancelled</response>
    /// <response code="404">Order not found</response>
    /// <response code="422">Order cannot be cancelled in its current status</response>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(CancelOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await _orderService.CancelAsync(id, request, cancellationToken);
        return Ok(result);
    }
}
