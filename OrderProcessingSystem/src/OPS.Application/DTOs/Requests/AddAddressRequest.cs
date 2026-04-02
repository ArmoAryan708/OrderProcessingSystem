namespace OPS.Application.DTOs.Requests;

public record AddAddressRequest(
    string Line1,
    string? Line2,
    string City,
    string State,
    string Zip,
    string Country
);
