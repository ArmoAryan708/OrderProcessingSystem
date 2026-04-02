using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OPS.Application.DTOs.Responses;
using OPS.Application.Interfaces.Services;

namespace OPS.Web.Controllers;

/// <summary>Product catalog endpoints</summary>
[ApiController]
[Route("api/v1/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService) => _productService = productService;

    /// <summary>List all available products</summary>
    /// <response code="200">Products retrieved</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var products = await _productService.GetAllAsync(cancellationToken);
        return Ok(products);
    }
}
