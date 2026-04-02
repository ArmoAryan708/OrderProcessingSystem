using OPS.Application.DTOs.Requests;
using OPS.Application.DTOs.Responses;
using OPS.Application.Interfaces.Repositories;
using OPS.Application.Interfaces.Services;
using OPS.Domain.Exceptions;

namespace OPS.Application.Services;

public class AuthService : IAuthService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(ICustomerRepository customerRepository, IJwtTokenService jwtTokenService)
    {
        _customerRepository = customerRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByEmailAsync(request.Email, cancellationToken)
            ?? throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Credentials"] = ["Invalid email or password."]
            });

        if (!BCrypt.Net.BCrypt.Verify(request.Password, customer.PasswordHash))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Credentials"] = ["Invalid email or password."]
            });

        var (token, expiresAt) = _jwtTokenService.GenerateToken(customer);
        return new AuthResponse(token, expiresAt, customer.Id, customer.Name);
    }
}
