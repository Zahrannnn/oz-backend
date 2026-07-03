using FluentValidation;
using Oz.Api.Controllers.Admin;

namespace Oz.Api.Validators;

public class ExchangeRequestValidator : AbstractValidator<ExchangeRequest>
{
    public ExchangeRequestValidator()
    {
        RuleFor(x => x.OrderItemId)
            .GreaterThan(0).WithMessage("OrderItemId must be greater than 0");

        RuleFor(x => x.NewVariantId)
            .GreaterThan(0).WithMessage("NewVariantId must be greater than 0");

        RuleFor(x => x.Qty)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters");
    }
}
