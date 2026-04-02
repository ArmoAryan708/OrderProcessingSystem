using Microsoft.AspNetCore.Mvc;
using OPS.Application.DTOs.Requests;
using OPS.Application.DTOs.Responses;
using OPS.Application.Interfaces.Services;

namespace OPS.Web.Controllers;

/// <summary>Authentication endpoints</summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICustomerService _customerService;

    public AuthController(IAuthService authService, ICustomerService customerService)
    {
        _authService = authService;
        _customerService = customerService;
    }

    /// <summary>Register a new customer account</summary>
    /// <response code="201">Customer registered successfully</response>
    /// <response code="400">Validation error or email already registered</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _customerService.RegisterAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Register), result);
    }

    /// <summary>Login and receive a JWT token</summary>
    /// <response code="200">Login successful</response>
    /// <response code="400">Invalid credentials</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        return Ok(result);
    }
}
