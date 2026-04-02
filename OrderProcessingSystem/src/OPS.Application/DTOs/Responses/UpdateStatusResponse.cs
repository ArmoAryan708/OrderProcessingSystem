using OPS.Domain.Enums;

namespace OPS.Application.DTOs.Responses;

public record UpdateStatusResponse(
    Guid Id,
    OrderStatus PreviousStatus,
    OrderStatus CurrentStatus,
    DateTimeOffset UpdatedAt
);
