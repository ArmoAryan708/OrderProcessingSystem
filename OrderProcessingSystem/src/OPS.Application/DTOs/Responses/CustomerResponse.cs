namespace OPS.Application.DTOs.Responses;

public record CustomerResponse(
    Guid Id,
    string Name,
    string Email,
    string? Phone,
    IReadOnlyList<AddressResponse> Addresses
);
