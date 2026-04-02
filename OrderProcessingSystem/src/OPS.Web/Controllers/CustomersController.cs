using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OPS.Application.DTOs.Requests;
using OPS.Application.DTOs.Responses;
using OPS.Application.Interfaces.Services;

namespace OPS.Web.Controllers;

/// <summary>Customer management endpoints</summary>
[ApiController]
[Route("api/v1/customers")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService) => _customerService = customerService;

    /// <summary>List all customers</summary>
    /// <response code="200">Customers retrieved</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var customers = await _customerService.GetAllAsync(cancellationToken);
        return Ok(customers);
    }

    /// <summary>Add a shipping address to a customer</summary>
    /// <response code="201">Address added</response>
    /// <response code="404">Customer not found</response>
    [HttpPost("{id:guid}/addresses")]
    [ProducesResponseType(typeof(AddressResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddAddress(Guid id, [FromBody] AddAddressRequest request, CancellationToken cancellationToken)
    {
        var address = await _customerService.AddAddressAsync(id, request, cancellationToken);
        return Created(string.Empty, address);
    }
}
