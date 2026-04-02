using OPS.Domain.Entities;

namespace OPS.Application.Interfaces.Services;

public interface IJwtTokenService
{
    (string token, DateTimeOffset expiresAt) GenerateToken(Customer customer);
}
