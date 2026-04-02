namespace OPS.Application.DTOs.Responses;

public record AuthResponse(string Token, DateTimeOffset ExpiresAt, Guid CustomerId, string Name);
