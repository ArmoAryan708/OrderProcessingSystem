using FluentValidation;
using OPS.Application.DTOs.Requests;
using OPS.Domain.Enums;

namespace OPS.Application.Validators;

public class UpdateOrderStatusValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid order status value.")
            .NotEqual(OrderStatus.Pending).WithMessage("Cannot manually set status back to PENDING.");
    }
}
