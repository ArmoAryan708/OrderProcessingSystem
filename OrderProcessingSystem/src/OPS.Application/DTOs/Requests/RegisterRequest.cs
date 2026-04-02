namespace OPS.Application.DTOs.Requests;

public record RegisterRequest(
    string Name,
    string Email,
    string Password,
    string? Phone = null
);
