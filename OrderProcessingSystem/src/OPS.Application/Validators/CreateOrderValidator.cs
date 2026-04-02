using FluentValidation;
using OPS.Application.DTOs.Requests;

namespace OPS.Application.Validators;

public class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.ShippingAddressId).NotEmpty();
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain at least one item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).NotEmpty();
            item.RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0.");
        });
    }
}
