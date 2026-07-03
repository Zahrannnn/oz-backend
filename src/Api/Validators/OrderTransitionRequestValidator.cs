using FluentValidation;
using Oz.Api.Controllers.Admin;

namespace Oz.Api.Validators;

public class OrderTransitionRequestValidator : AbstractValidator<OrderTransitionRequest>
{
    public OrderTransitionRequestValidator()
    {
        RuleFor(x => x.ToState)
            .NotEmpty().WithMessage("ToState is required");
    }
}
