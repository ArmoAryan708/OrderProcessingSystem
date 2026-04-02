using FluentAssertions;
using OPS.Application.DTOs.Requests;
using OPS.Application.Validators;

namespace OPS.UnitTests.Validators;

public class CreateOrderValidatorTests
{
    private readonly CreateOrderValidator _validator = new();

    [Fact]
    public async Task Validate_ValidRequest_PassesValidation()
    {
        var request = new CreateOrderRequest(
            Guid.NewGuid(), Guid.NewGuid(),
            [new OrderItemRequest(Guid.NewGuid(), 2)]);

        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyItems_FailsValidation()
    {
        var request = new CreateOrderRequest(Guid.NewGuid(), Guid.NewGuid(), []);

        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Items");
    }

    [Fact]
    public async Task Validate_ZeroQuantity_FailsValidation()
    {
        var request = new CreateOrderRequest(
            Guid.NewGuid(), Guid.NewGuid(),
            [new OrderItemRequest(Guid.NewGuid(), 0)]);

        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Quantity"));
    }

    [Fact]
    public async Task Validate_EmptyCustomerId_FailsValidation()
    {
        var request = new CreateOrderRequest(
            Guid.Empty, Guid.NewGuid(),
            [new OrderItemRequest(Guid.NewGuid(), 1)]);

        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerId");
    }
}
