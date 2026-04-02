namespace OPS.Application.DTOs.Responses;

public record CancelOrderResponse(
    Guid Id,
    string Status,
    DateTimeOffset CancelledAt,
    string? Reason
);
