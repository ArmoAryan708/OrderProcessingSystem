using OPS.Application.DTOs.Requests;
using OPS.Application.DTOs.Responses;

namespace OPS.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
